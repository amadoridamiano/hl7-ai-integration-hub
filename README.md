# HL7 AI Integration Hub
A comprehensive solution for generating, sending, receiving, and testing HL7 messages using OpenAI GPT and the TCP protocol.

## AI Agent Role

This solution defines a task-oriented AI agent powered by OpenAI Assistants v2.
The agent is designed to generate valid, compliant HL7 messages using uploaded healthcare documentation, which is indexed via vector stores and queried in real time.

## Solution Components

### 1. **HL7MockGenerator**
- Generates HL7 messages using OpenAI models.
- Configurable via the `appsettings.json` file to specify the model, version, type of HL7 message.
- Instructions for the assistants are customizable via the `instructions.txt` file.

### 2. **OpenAIServices**
- Manages API calls to OpenAI Assistants v2.
- Creates vector stores and uploads PDF/TXT documentation.
- Associates vector stores with assistants to enable file_search.
- Includes API key validation and message generation based on prompts.

### 3. **HL7Services**
- Handles sending HL7 messages to a server using the MLLP (TCP) protocol.
- Receives and displays ACK responses.

## Requirements

- .NET 9
- A valid OpenAI API key with access to Assistants API v2

## Configuration

The main configuration is located in `HL7MockGenerator/appsettings.json`. Example:

```json
{
  "OpenAI": {
    "Model": "gpt-4"
  },  
  "Connection": {
    "Ip": "192.168.1.60",
    "Port": "55656"
  },
  "Message": {
    "Version": "2.5",
    "Type": "ADT^A31"
  },
  "Mode": "2",
  "SpecsPath": "C:\\Lab\\Specs"
}
```

## Key Parameters:
- **OpenAI:Model**: Specifies the OpenAI model to use.
- **Connection:Ip** and **Connection:Port**: HL7 server IP address and port.
- **Message:Version** and **Message:Type**: HL7 message version and type.
- **Mode**: Operational mode (e.g., generation or sending).
- **SpecsPath**: Folder path containing documentation (TXT higher performance) to upload to OpenAI vector store.

## Features

- Uses OpenAI Assistants v2 with vector stores for domain-aware HL7 message generation.
- Supports PDF and TXT file upload for use in assistant file_search.
- Automatically handles creation of vector store and assistant.
- Post-processing of GPT output to ensure HL7 segments end with CRLF (`\r\n`).

## Getting Started

1. Build the solution:
    ```bash
    dotnet build
    ```

2. Run the generator:
    ```bash
    dotnet run --project HL7MockGenerator
    ```

## Solution Structure

- `HL7MockGenerator`: Main project for message generation and sending.
- `OpenAIServices`: Library for OpenAI Assistants v2 integration and vector store management.
- `HL7Services`: Library for HL7 TCP/MLLP protocol handling.