# Use an official AWS Lambda Python base image
FROM public.ecr.aws/lambda/python:3.8

# Install necessary dependencies
COPY requirements.txt .
RUN pip install --upgrade pip
RUN pip install -r requirements.txt

# Copy the rest of the application code
COPY main.py .

# Set the CMD to your handler (default lambda handler entry point)
CMD ["main.handler"]


FROM public.ecr.aws/lambda/python:3.10
# Copy function code
COPY ./app ${LAMBDA_TASK_ROOT}
# Install the function's dependencies using file requirements.txt
# from your project folder.
COPY requirements.txt .
RUN pip3 install -r requirements.txt - target "${LAMBDA_TASK_ROOT}" -U - no-cache-dir
# Set the CMD to your handler (could also be done as a parameter override outside of the Dockerfile)
CMD [ "main.handler" ]
