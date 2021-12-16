### Working with docker.

There are some prerequisites for using the included Dockerfile and docker-compose.yml files:

1) Make sure you have docker installed (on windows install docker desktop)

2) Create and install an https certificate:

    ```
    dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\DN.Host.pfx -p SuperSecurePassword123!
    ```

3) It's possible that the above step gives you an `A valid HTTPS certificate is already present.` error.
   In that case you will have to run this first:

    ```
     dotnet dev-certs https --clean
    ```

4) Trust the certificate

    ```
     dotnet dev-certs https --trust
    ```


After that you should be able to run

     docker-compose up -d --build

from the root project folder and if everything builds fine, your api should be available at `https://localhost:5060/swagger`

**!! There are more docker-compose examples under the deployments folder !!**