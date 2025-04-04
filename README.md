# Ofqual Register of Recognised Qualifications Citizen API

[![Build Status](https://ofqual.visualstudio.com/Ofqual%20IM/_apis/build/status%2Fofqual-recognition-citizen-api?branchName=main)](https://ofqual.visualstudio.com/Ofqual%20IM/_build/latest?definitionId=395&branchName=main)

The Ofqual Register of Recognised Qualifications Citizen API is used by the Register of Recognised Qualifications web application to allow the following:

- Submission of applications to be recognised
- Retrieve currently incomplete applications to allow an applicant to continue their application
- Find out if a qualification can be recognised

## Provider

[The Office of Qualifications and Examinations Regulation](https://www.gov.uk/government/organisations/ofqual)

## About this project

This project is a ASP.NET Core 8 web api utilising Docker for deployment.

The web api runs on an App service for Container apps on Azure.

### App Settings Definition

The following configuration structure is used in `appsettings.json`. Each section defines settings that control application behaviour across different environments.

```json
{
  "LogzIo": {
    "Environment": "",
    "Uri": ""
  },
  "ConnectionStrings": {
    "OfqualODS": ""
  }
}
```

#### Setting Descriptions

- `LogzIo:Environment`  
  A label for identifying the current environment (e.g., DEV, PREPROD, PROD) in logs.

- `LogzIo:Uri`  
  The endpoint URI for sending logs to Logz.io or another external logging service.

- `ConnectionStrings:OfqualODS`  
   The connection string used to connect to the Ofqual ODS database.

> These settings should be environment-specific and managed through `appsettings.{Environment}.json` or overridden using environment variables in production scenarios.

### Test Settings Definition

The following configuration structure is used in `appsettings.Test.json`. These settings are specifically for **integration testing** scenarios and help define the runtime context for test containers, database setup and connection handling.

```json
{
  "TestSettings": {
    "RegistryEndpoint": "",
    "ImagePath": "",
    "RegistryUsername": "",
    "RegistryPassword": "",
    "SqlUsername": "",
    "SqlPassword": "",
    "DatabaseName": ""
  }
}
```

#### Setting Descriptions

- `TestSettings:RegistryEndpoint`  
  The container registry URL where the database image is hosted (e.g., Docker Hub, AWS ECR, Azure Container Registry).

- `TestSettings:ImagePath`  
  The image path (repository name) for the test database container. It will be combined with the registry to pull the full image.

- `TestSettings:RegistryUsername`  
  Username for authenticating with the container registry.

- `TestSettings:RegistryPassword`  
  Password or token used for authenticating with the container registry.

- `TestSettings:SqlUsername`  
  The username used to connect to the SQL Server running in the test container.

- `TestSettings:SqlPassword`  
  The password associated with the SQL user.

- `TestSettings:DatabaseName`  
   The name of the database to be created or used in the test container.

> These settings are used only for **automated integration tests** and should not be applied to production configurations. They can be overridden via environment variables in CI/CD pipelines using the format: `TestSettings__YourCustomVariable`.
