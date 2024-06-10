import logging
import sys

from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from pydantic import BaseModel
from typing import Any, Dict
import tempfile
import os
import json
import importlib.util
from contextlib import contextmanager
from mangum import Mangum

# Set up logging
logger = logging.getLogger()
logger.setLevel(logging.INFO)
handler = logging.StreamHandler(sys.stdout)
handler.setLevel(logging.INFO)
formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
handler.setFormatter(formatter)
logger.addHandler(handler)

app = FastAPI()


class Config(BaseModel):
    input: str
    variables: Dict[str, Any]
    config: Dict[str, Any]
    env: Dict[str, Any]


@contextmanager
def set_env_vars(env_vars: Dict[str, str]):
    original_env_vars = {key: os.environ.get(key) for key in env_vars}
    os.environ.update(env_vars)
    try:
        yield
    finally:
        for key, value in original_env_vars.items():
            if value is None:
                del os.environ[key]
            else:
                os.environ[key] = value


def load_module_from_file(file_path):
    spec = importlib.util.spec_from_file_location("module.name", file_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


@app.post("/prompt")
async def prompt(file: UploadFile = File(...), config: str = Form(...)):
    logger.info("Received request with file: %s", file.filename)
    # Step 1: Check file extension
    if not file.filename.endswith(".py"):
        logger.error("Invalid file extension")
        raise HTTPException(status_code=400, detail="Only Python files are allowed.")

    # Step 2: Parse JSON config
    try:
        config_data = json.loads(config)
    except json.JSONDecodeError:
        logger.error("Invalid JSON in config")
        raise HTTPException(status_code=400, detail="Invalid JSON in config.")
    config = Config(**config_data)

    # Flatten the config to include input, variables, and env in one dictionary
    flattened_config = {"input": config.input}
    flattened_config.update(config.variables)

    # Step 3: Save file to temporary location
    with tempfile.NamedTemporaryFile(delete=False, suffix=".py") as temp_file:
        temp_file.write(await file.read())
        temp_file_path = temp_file.name
    logger.info("Saved file to temporary location: %s", temp_file_path)

    # Step 4: Execute the chain with the config and context manager
    try:
        with set_env_vars(config.env):
            module = load_module_from_file(temp_file_path)

            if not hasattr(module, 'chain'):
                logger.error("The provided Python file does not contain 'chain' variable")
                raise HTTPException(status_code=400,
                                    detail="The provided Python file does not contain 'chain' variable.")

            chain = module.chain
            #
            result = chain.invoke(flattened_config)
        output = result
    except Exception as e:
        logger.error("Error during execution: %s", str(e))
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        os.remove(temp_file_path)
        logger.info("Temporary file removed: %s", temp_file_path)

    return {"result": output}


handler = Mangum(app)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
