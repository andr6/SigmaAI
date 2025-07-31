# IntelliLink AI Platform

![Build](https://img.shields.io/github/actions/workflow/status/ElderJames/SigmaAI/dotnet.yml?style=flat-square)

IntelliLink is an extensible enterprise platform combining large language models (LLMs) with a plugin driven .NET 8 application.  The core goal is to expose advanced AI capabilities as easily consumable APIs so existing business systems can become intelligence enabled.

## Features

- **LLM Integration**  
  - OpenAI compatible REST endpoints.
  - Local model support through **LLamaSharp** and **Ollama**.
  - Function Calling via OpenAI/SparkDesk/DashScope or intent based recognition when native APIs are unavailable.
- **Plugins and Native Functions**  
  - Semantic Kernel based plugin system for calling external REST APIs or annotated C# methods.  
  - Built‑in plugins:
    - *CyberIntelPlugin* – extract IOCs from CTI PDFs and generate detection rules.
    - *ThreatIntelPlugin* – create threat intelligence reports from CVE or vulnerability data.
    - *SecurityAssessmentPlugin* – parse nmap output and recommend controls.
    - *CyberRangeFunctions* – drive a GAN cyber‑range simulator.
- **Knowledge Driven RAG** using Kernel Memory for document indexing and retrieval.
- **Security Assessment Module** leveraging reinforcement learning agents to update policies.
- **Threat Intel Backend**  
  - FastAPI service ingesting indicators from OTX, Shodan, Censys and VirusTotal.  
  - IOC summaries produced with the configured LLM provider.
- **React/Next.js Frontend** showing live IOC feeds with AI generated summaries.
- **Auxiliary Tools**  
  - `scripts/threat_report_parser.py` to convert CTI reports to JSON or STIX.  
  - `scripts/gan_range_runner.py` and `cyberintel_runner.py` command line helpers.

## Installation

### Prerequisites

- [.NET SDK 8](https://dotnet.microsoft.com/download)
- Python 3.8+
- Node.js 18+
- Optional API keys: `OPENAI_API_KEY`, `OTX_API_KEY`, `SHODAN_API_KEY`, `CENSYS_UID`, `CENSYS_SECRET`, `VT_API_KEY`.

### Steps

```bash
# clone repository
git clone https://github.com/ElderJames/SigmaAI.git
cd SigmaAI
```

1. **Run the .NET server**
   ```bash
   dotnet run --project src/Sigma
   ```
2. **Start the Python backend**
   ```bash
   pip install -r backend/requirements.txt
   uvicorn backend.main:app --reload
   ```
3. **Launch the React frontend**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

Navigate to the printed URL to access the web interface.

## Development Notes

- Main stack: **.NET 8**, **EF Core**, **Blazor**, **Semantic Kernel** and **Kernel Memory**.
- Unit tests reside under `tests/` and can be executed with `dotnet test`.
- Python utilities rely on the packages listed in `backend/requirements.txt`.

## License

IntelliLink is released under the Apache 2.0 license.
