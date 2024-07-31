# Deploying FullStackHero .NET Starter Kit to AWS with Terraform

## Pre-Requisites

1. Ensure you have the latest Terraform installed on your machine. I used `Terraform v1.8.4` at the time of development.
2. You should have installed AWS CLI Tools and authenticated your machine to access AWS.

## Bootstrapping

If you are deploying this for the first time, navigate to `./boostrap` and run the following commands

```terraform
terraform init
terraform plan
terraform apply --auto-approve
```

This will provision the required backend resources on AWS that terraform would need in the next steps. Basics this will create an Amazon S3 Bucket, and DynamoDB Table.

## Deploy

Navigate to `./environments/dev/` and run the following.

```
terraform init
terraform plan
terraform apply --auto-approve
```

This will deploy the following,

- ECS Cluster and Task (.NET Web API)
- RDS PostgreSQL Database Instance
- Required Networking Components like VPC, ALB etc.

## Destroy

Once you are done with your testing, ensure that you delete the resources to keep your bill under control.

````
terraform destroy --auto-approve
```
````
