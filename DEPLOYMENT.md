# Deployment Notes

This app now uses SQLite locally through `myApp/App_Data/trips.db`.

## No-payment hosting path

Render has a free web service tier that can deploy this project from GitHub using the included `Dockerfile` and `render.yaml`.

Important limitation: Render free web services use an ephemeral filesystem. SQLite data can disappear when the service restarts, redeploys, or spins down. This is fine for a free demo, but not for permanent production data.

## Render steps

1. Push this folder to GitHub.
2. Sign in to Render.
3. Create a new Blueprint or Web Service from the GitHub repo.
4. Choose the free instance type.
5. Deploy.

## Azure alternative

Azure App Service has a free tier for learning and experiments. It is a better fit for ASP.NET Core, but deployment needs an Azure sign-in and account setup.
