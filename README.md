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

# Application Configuration Guide

This document outlines how the application is configured using `appsettings.json` files. These settings help manage behaviour across different environments and scenarios, including development, production and automated testing.

## Application Settings (`appsettings.json`)

The main application settings are defined in `appsettings.json` and can be tailored per environment using files like `appsettings.Development.json` or `appsettings.Production.json`.

```json
{
  "LogzIo": {
    "Environment": "",
    "Uri": ""
  },
  "AntiVirus": {
    "BaseUri": "",
    "AuthToken": ""
 }
  "ConnectionStrings": {
    "OfqualODS": ""
  }
}
```

### Setting Details

- **`LogzIo:Environment`**  
  Identifies the current environment in the logs (e.g., `DEV`, `PREPROD`, `PROD`). This helps differentiate log entries across deployments.

- **`LogzIo:Uri`**  
  The endpoint for sending log data to an external logging service such as Logz.io.

- `AntiVirus:BaseUri`  
  The base address of the external anti-virus scanning API (e.g., `https://api.attachmentscanner.com`).

- `AntiVirus:AuthToken`
  The bearer token used to authenticate with the anti-virus scanning service.

- **`ConnectionStrings:OfqualODS`**  
  Connection string for accessing the Ofqual ODS (Organisational Data Service) database.

> It’s recommended to manage environment-specific values in `appsettings.{Environment}.json` or override them via environment variables, especially in production.

## Test Settings (`appsettings.Test.json`)

Used only for **automated integration tests**, these settings configure test containers and database access. They are **not** meant for production environments.

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

### Setting Details

- **`TestSettings:RegistryEndpoint`**  
  The container registry URL where the database image is stored (e.g., Docker Hub, AWS ECR).

- **`TestSettings:ImagePath`**  
  The repository path for the test database image. Combined with the registry URL to pull the image.

- **`TestSettings:RegistryUsername`**  
  Username for accessing the container registry.

- **`TestSettings:RegistryPassword`**  
  Password or token for authenticating with the container registry.

- **`TestSettings:SqlUsername`**  
  SQL Server username for connecting to the test database container.

- **`TestSettings:SqlPassword`**  
  Corresponding password for the SQL user.

- **`TestSettings:DatabaseName`**  
   The name of the test database to be created or accessed during integration tests.

> These settings support the test environment and can be overridden in CI/CD using environment variables (e.g., `TestSettings__DatabaseName`).
