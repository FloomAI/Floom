<div align="center">


<img width="60%" src="/ReadmeAssets/GitHub-Logo.png" title="Floom - AI Orchestration" />

![25github-text.jpg](/ReadmeAssets/25github-text.jpg) **Floom** orchestrates and executes Generative AI pipelines, <br/>Empowering **Developers** to <ins>focus on what matters</ins>.
<br/><br/> ✅ Enterprise-Grade ✅ Production-Ready ✅ Battle-Tested
<br/> ![25github-text.jpg](/ReadmeAssets/25github-text.jpg) **Floom** is now Open-Source and **100% FREE** for everyone (including commercial use).
  
![Version](https://img.shields.io/badge/version-1.1.7-blue)
![License](https://img.shields.io/badge/license-MIT-green)

</div>

<p align="center">
    <img width="100%" src="/ReadmeAssets/Define.jpg" title="Define Generative AI Pipelines: Text Completion, Chat, Images, Video, Audio & Speech" />
</p>
<p align="center">
    <img width="100%" src="/ReadmeAssets/SDK-+-Cost.jpg" title="Integrate quickly with friendly SDKs (Node, Python, .NET, Java, Go & PHP), Cost Management & Protection per product, user, session or context" />
</p>
<p align="center">
    <img width="100%" src="/ReadmeAssets/Data-+-Updates.jpg" title="Connect to multiple data sources: Files, Databases, APIs with automatic updates & version control" />
</p>
<p align="center">
    <img width="100%" src="/ReadmeAssets/Caching-Robustness.jpg" title="Caching to reduce model costs and latency, Robustness with automatic failover to other models" />
</p>
<p align="center">
    <img width="100%" src="/ReadmeAssets/Security-+-Safety.jpg" title="Security with DLP (Data Leakage Prevention), Token-Abuse, API-Abuse, Data-Exfiltration, Prompt Injection Protection, Safety with guardrails, filter curse words, illegal materials etc" />
</p>

------
❓**What's AI Pipeline?** 
------
**"AI Pipeline" is the definition of the entire GenAI integration & execution process.** In order to properly execute a production-grade, cost-effective Generative AI integration, organizations need to: design a prompt, inject dynamic variables, link data, augment with RAG, compress, execute, format I/O, optimize, cache, re-execute, validate, safeguard, monitor, log, manage costs & much more. 
<br/><br/>This is Floom's **AI Pipeline** model:
![PipelineDefintion.jpg](/ReadmeAssets/PipelineDefintion.jpg)

------
### **![25github-text.jpg](/ReadmeAssets/25github-text.jpg) Floom handles most of this hassle for you. This is ![25github-text.jpg](/ReadmeAssets/25github-text.jpg) Floom's magic.**

![Flow.jpg](/ReadmeAssets/Flow.jpg)
------

### Launch &nbsp; ![25github-text.jpg](/ReadmeAssets/25github-text.jpg) **Floom**
1. Download &nbsp; ![25github-text.jpg](/ReadmeAssets/25github-text.jpg) **Floom**'s 'Docker-Compose' file
```
curl -O https://github.com/FloomAI/Floom/blob/master/docker-compose.yml
```
2. Run &nbsp; ![25github-text.jpg](/ReadmeAssets/25github-text.jpg) **Floom**
```
docker compose up -d
```
------
### Create your first **AI Pipeline**
Each pipeline is defined with straightforward YAML files. It's a perfect and robust option for enterprise dev/devops teams who rely on streamlined & ever-changing CI/CD and wish to keep track or easily modify anything in their pipeline, without changing code.

### Example #1: Using OpenAI to chat with a PDF file

Clone example definition files [See here](https://github.com/FloomAI/Floom/tree/master/Tests/Examples/Docs%20Pipeline):
```
git clone https://github.com/FloomAI/Floom.git
cd Floom/Tests/Examples/Docs-Pipeline
```

Or use FloomCLI to generate these files:
```
pip install Floom
floom -example=docs-pipeline
```

Or generate manually:

1. Create a new folder for your pipeline ("docs-pipeline")
2. Create a new **Pipeline** definition file: "docs-pipeline.yml".
```
schema: v1
kind: Pipeline
id: docs-pipeline
model: docs-model
prompt: docs-prompt
response: docs-response
data:
- docs-data
```
3. Now define the AI model in you pipeline. Create a new **Model** definition file: "docs-model.yml"
```
schema: v1
kind: Model
id: docs-model
vendor: OpenAI
model: gpt-3.5-turbo
apiKey: sk-8j84j8f48j824j8g2h58gh82h82h57gh27g
```
4. Create a new **Prompt** definition file: "docs-prompt.yml"
```
schema: v1
kind: Prompt
id: docs-prompt
type: text
system: "You are a helpful assitant. Start chat with the user's first name which is {{first_name}}."
```
5. Create a new **Response** definition file: "docs-response.yml"
```
schema: v1
kind: Response
id: docs-response
type: text  
language: English
maxSentences: 3
maxCharacters: 1500
temperature: 0.9
```
6. Create a new **Data** definition file: "docs-data.yml"
```
schema: v1
kind: Data
id: docs-data
type: file
path: /dev/test/documentation.pdf
split: pages
embeddings: docs-embeddings
```
7. Create a new **Embeddings** defintion file: "docs-embeddings.yml"
```
schema: v1
kind: Embeddings
id: docs-embeddings
type: text  
vendor: OpenAI
model: text-embedding-ada-002
apiKey: sk-924f2j8h27vh8j8hr7hv8w8j85w8h8w85gjs8
```

### Commit your **AI Pipeline**

Use Floom CLI to commit your new GenAI pipeline:

```floom apply -d my-pipeline```
Note that "my-pipeline" is the name of the directory.

### Execute your **AI Pipeline**

Get Floom SDK for your favorite framework and run the pipeline.

Check out this Python example:

Initiate Floom client:
```
floom_client = FloomClient(
    endpoint="http://localhost:4050",
    api_key="Vcm6RReBMLQa0h2fUidOC7SLmh356uHH"
)
```

Run pipeline:
```
x = floom_client.run(
    pipelineId="docs-pipeline",
    input="How do I reset the oil alert in my dashboard?"
)
```

Print response values:
```
print(x.values[0]['value'])
```
