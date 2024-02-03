//// setup.ts

//import { FloomPrompt, PipelineConfig, DataConfig } from './your-sdk'; // Replace './your-sdk' with the actual path to your SDK

//async function main() {
//    // Replace '<apikey>', '<your id here>', and '<path-to-pdf>' with actual values
//    const apiKey = '824jf285hg828gj2g951gh18';
//    const yourId = '<your id here>';
//    const pdfPath = '/dev/test/documentation.pdf';

//    // Create a new FloomPrompt instance with the API key
//    const fp = new FloomPrompt(apiKey);

//    // Create a Pipeline configuration
//    const pipelineConfig: PipelineConfig = {
//        schema: 'v1',
//        kind: 'Pipeline',
//        name: 'docs-pipeline',
//        chatHistory: true,
//        model: {
//            vendor: 'OpenAI',
//            model: 'davinci-003',
//            apiKey: apiKey,
//        },
//        prompt: {
//            type: 'text',
//            system: "You are an assistant that speaks like Shakespeare",
//            user: "{input}",
//        },
//        response: {
//            type: 'text',
//            language: 'English',
//            maxSentences: 3,
//            maxCharacters: 1500,
//            temperature: 0.9,
//        },
//        data: [
//            {
//                type: 'file',
//                path: pdfPath,
//                split: 'pages',
//                embeddings: {
//                    type: 'text',
//                    vendor: 'OpenAI',
//                    model: 'text-embedding-ada-002',
//                    apiKey: apiKey,
//                },
//                vectorStore: {
//                    vendor: 'Pinecone',
//                    apiKey: apiKey,
//                },
//            },
//        ],
//    };

//    try {
//        // Create the Pipeline using the provided yourId and pipelineConfig
//        await fp.Pipeline(yourId, pipelineConfig);
//        console.log('Pipeline created successfully!');
//    } catch (error) {
//        console.error('Failed to create pipeline:', error);
//    }
//}

//main();
