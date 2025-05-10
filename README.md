# HL7 AI Integration Hub
A comprehensive solution for generating, sending, receiving, and testing HL7 messages using OpenAI GPT and the TCP protocol.

## Solution Components

### 1. **HL7MockGenerator**
- Generates HL7 messages using OpenAI models.
- Configurable via the `appsettings.json` file to specify the model, version, and type of HL7 message.

### 2. **OpenAIServices**
- Manages API calls to OpenAI for content generation.
- Includes API key validation and message generation based on prompts.

### 3. **HL7Services**
- Handles sending HL7 messages to a server using the MLLP (TCP) protocol.
- Receives and displays ACK responses.

## Requirements

- .NET 9
- A valid OpenAI API key

## Configuration

The main configuration is located in `HL7MockGenerator/appsettings.json`. Example:

```json
{
  "OpenAI": {
    "Model": "gpt-4o-mini"
  },
  "Connection": {
    "Ip": "127.0.0.1",
    "Port": "2575"
  },
  "Message": {
    "Version": "2.5",
    "Type": "ADT^A31"
  },
  "Mode": "1"
}
```

## Key Parameters:
- **OpenAI:Model**: Specifies the OpenAI model to use.
- **Connection:Ip** and **Connection:Port**: HL7 server IP address and port.
- **Message:Version** and **Message:Type**: HL7 message version and type.
- **Mode**: Operational mode (e.g., generation or sending).

## Getting Started

1. Build the solution:
    ```shell
    dotnet build
    ```
2. Run project (example for HL7MockGenerator):
    ```shell
    dotnet run --project HL7MockGenerator
    ```

## Solution Structure

- `HL7MockGenerator`: Main project for message generation and sending.
- `OpenAIServices`: Library for OpenAI integration.
- `HL7Services`: Library for HL7 protocol handling.