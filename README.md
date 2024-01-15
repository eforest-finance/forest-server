# forest-server
BRANCH | AZURE PIPELINES                                                                                                                                                                                                                               | TESTS                                                                                                                                                                                                | CODE COVERAGE
-------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------
MASTER   | [![Build Status](https://dev.azure.com/eforest-finance/forest-server/_apis/build/status/eforest-finance.forest-server?branchName=master)](https://dev.azure.com/eforest-finance/forest-server/_build/latest?definitionId=2&branchName=master) | [![Test Status](https://img.shields.io/azure-devops/tests/eforest-finance/forest-server/2/main)](https://dev.azure.com/eforest-finance/forest-server/_build/latest?definitionId=2&branchName=master) | [![codecov](https://codecov.io/gh/eforest-finance/forest-server/branch/main/graph/badge.svg?token=1OLJV6TWRB)](https://codecov.io/gh/eforest-finance/forest-server)
DEV    | [![Build Status](https://dev.azure.com/eforest-finance/forest-server/_apis/build/status/eforest-finance.forest-server?branchName=dev)](https://dev.azure.com/eforest-finance/forest-server/_build/latest?definitionId=2&branchName=dev)       | [![Test Status](https://img.shields.io/azure-devops/tests/eforest-finance/forest-server/2/dev)](https://dev.azure.com/eforest-finance/forest-server/_build/latest?definitionId=2&branchName=dev)     | [![codecov](https://codecov.io/gh/eforest-finance/forest-server/branch/dev/graph/badge.svg?token=1OLJV6TWRB)](https://codecov.io/gh/eforest-finance/forest-server)



# 一、 Use Apollo

## 1.1 build apollo configuration center

For more details of the product introduction, please refer [Introduction to Apollo Configuration Center](https://www.apolloconfig.com/#/zh/design/apollo-introduction).

For local demo purpose, please refer [Quick Start](https://www.apolloconfig.com/#/zh/deployment/quick-start).

https://www.apolloconfig.com/#/en/design/apollo-introduction

## 1.2 modify apollosettings.json

AppId: The identity information of the application is an important information for obtaining the configuration from the server

MetaServer：Connect to the server through MetaServer to obtain configuration, no need to configure Env)

Namespaces: Apollo configuration center configures different namespaces

``` json
{
  "apollo": {
    "AppId": "server1",
    "MetaServer": "http://metaServer:8080,http://metaServer:8081",
    "Namespaces": [
      "Silo.appsettings.json"
    ]
  }
}
```