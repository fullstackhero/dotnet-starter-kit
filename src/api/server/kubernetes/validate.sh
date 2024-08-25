#!/bin/bash
helm template maxiar-dotnetstarterkit-webapi ./helm -f ./helm/values-dev.yaml >  maxiar-dotnetstarterkit-webapi.yaml