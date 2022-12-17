# Play.Catalog
Trading microservice.

## Build the docker image
```powershell
$version="1.0.1"
$env:GH_OWNER="RafaelJCamara"
$env:GH_PAT="[PERSONAL ACCESS TOKEN HERE]"
$appname="playeconomy"

docker build --secret id=GH_OWNER --secret id=GH_PAT -t "$appname.azurecr.io/play.trading:$version" .
```

## Run the docker image
```powershell
$version="1.0.1"
$cosmosDbConnString="[CONN STRING HERE]"
$serviceBusConnString="[CONN STRING HERE]"

docker run -it --rm -p 5006:5006 --name trading -e MongoDbSettings__ConnectionString=$cosmosDbConnString -e ServiceBusSettings__ConnectionString=$serviceBusConnString -e ServiceSettings__MessageBroker="SERVICEBUS" play.trading:$version
```

## Publish docker image
```powershell
az acr login --name $appname
docker push "$appname.azurecr.io/play.trading:$version"
```