<h1 align="center">🤖 IntelliLink AI Platform</h1>

<div align="center">

An out-of-the-box enterprise platform built on LLM and GenAI.

Our goal is to empower your business systems with intelligence for greater value.

![Build](https://img.shields.io/github/actions/workflow/status/ElderJames/SigmaAI/dotnet.yml?style=flat-square)

</div>

*Note: this project is under active development. Avoid production use before the official release.*

## ✨ Features

- Easy integration via WebAPI or native functions.
- Supports all OpenAI REST protocols.
- Local model integration through LLamaSharp or Ollama.
- Native Function Calling APIs like OpenAI, SparkDesk and DashScope.
- Function Calling via intent recognition when no native API is available.
- Retrieval augmented generation (RAG) from knowledge bases.
- Built on an early version of AntSK.

## 📦 Installation


- Install [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/8.0?WT.mc_id=DT-MVP-5003987).

- Clone and run the project

  ```bash
  $ git clone https://github.com/ElderJames/SigmaAI.git
  $ cd SigmaAI
  $ dotnet run --project src/sigma
  ```

- Finally create a user and enjoy!

## 🔨 Development

- Tech stack
  - Built with .NET 8, EF Core and Blazor.
  - UI based on Ant Design Blazar.
  - Semantic Kernel powers the LLM integration.
  - Kernel Memory handles RAG splitting, indexing and search.

## 🤝 贡献

[![PRs welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](https://github.com/ElderJames/SigmaAI/pulls)

Feel free to create a [Pull Request](https://github.com/ElderJames/SigmaAI/pulls) or submit a [Bug Report](https://github.com/ElderJames/SigmaAI/issues/new).


## 💕 Contributors

Thanks to all contributors

<a href="https://github.com/ElderJames/SigmaAI/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=ElderJames/SigmaAI&max=1000&columns=15&anon=1" />
</a>

## 🚨 Code of Conduct

This project follows the Contributor Covenant to define expected behavior.
See the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct) for details.

## ☀️ License

Licensed under the Apache 2.0 license.