import os
from langchain_openai import ChatOpenAI
from langchain_core.output_parsers import StrOutputParser
from langchain_core.prompts import ChatPromptTemplate

OPENAI_API_KEY = os.environ.get('OPENAI_API_KEY')
OPENAI_MODEL = os.environ.get('OPENAI_MODEL') or "gpt-3.5-turbo"

system_template = "Translate the following into {language}:"

prompt_template = ChatPromptTemplate.from_messages(
    [("system", system_template), ("user", "{input}")]
)

llm = ChatOpenAI(model=OPENAI_MODEL, api_key=OPENAI_API_KEY)

parser = StrOutputParser()

chain = prompt_template | llm | parser
