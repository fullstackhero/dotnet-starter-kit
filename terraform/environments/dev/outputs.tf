output "webapi" {
  value = "http://${module.webapi.endpoint}:8080"
}

output "blazor" {
  value = "http://${module.blazor.endpoint}"
}
