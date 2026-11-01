# GeoConquest

A Risk-style single-player geopolitics strategy game built with ASP.NET Core MVC, SignalR and MongoDB.

![CI](https://github.com/pfunzom61-cmd/GeoConquest/actions/workflows/ci.yml/badge.svg)

## How to run

_To be completed._

```
dotnet build
dotnet test
dotnet run --project src/GeoConquest.Web
```

## Marker's Guide

_To be completed._

## Architecture

_To be completed._

## Database Design

_To be completed._

## Rule interpretations

_To be completed._

## AI

**Battle commentary.** Every dice round gets one or two sentences of sports-style commentary from a language model
(Groq's OpenAI-compatible chat API, model `openai/gpt-oss-20b` by default).

- **Built from facts only.** The prompt is made from `BattleContext` as plain sentences: who rolled what, who lost a
  soldier and who is ahead in the attack. It asks for no numbers in the answer, because the model confused die values
  with dice counts. Player names are not in it, so nothing a user typed reaches the model.
- **Never slows play.** A roll is saved and returned straight away, and commentary is written by a background worker
  within a 2.5 second budget. It goes into a log position reserved with the roll, so it needs no second save of the
  game, cannot conflict with the next move, and reads in the right order however late it arrives.
- **Always available.** `ResilientCommentaryService` falls back to `TemplateCommentaryService` on a timeout, a 429, an
  error or a missing key. It stays within a per-minute limit and honours `retry-after`. Fast-forwarded NPC rolls use
  the template without spending a request.
- **Output is untrusted.** `CommentaryOutputGuard` cuts text back to two whole sentences and 240 characters, and
  rejects anything empty, off-topic, containing a digit or number word, or unsuitable for a family audience. The board
  only inserts it as plain text.
- **Persisted and labelled.** Commentary is a `Commentary` event, so spectators and reloads see it, and each line says
  whether the model or a template wrote it.

The API key comes from `dotnet user-secrets` (`Groq:ApiKey`) or the `Groq__ApiKey` environment variable. It is never
logged: the HTTP client's request logging is removed, and the service logs status codes only.

## Security

_To be completed._

## Deployment

The app runs as one Docker container built from the [Dockerfile](Dockerfile), which publishes `src/GeoConquest.Web`.
It listens on the `PORT` environment variable (Render sets it; the default is 8080).

**Environment variables** (names only; put the values in the host's dashboard, never in the repository):

| Variable | Needed | Purpose |
|---|---|---|
| `MongoDb__ConnectionString` | required | MongoDB Atlas connection string. The app refuses to start without it. |
| `MongoDb__DatabaseName` | optional | Database name. Defaults to `geoconquest`. |
| `ASPNETCORE_ENVIRONMENT` | optional | Set to `Production` on the host. |
| `Groq__ApiKey` | optional | Turns on AI commentary. Without it every line comes from the template. |
| `Groq__Model` | optional | Groq model name. Defaults to `openai/gpt-oss-20b`. |

`PORT` is set by the host and is not something to configure. `ForwardedHeaders__ForwardLimit` is only needed if two
different visitors share one login rate-limit bucket (raise it from 1 to 2).

**Render settings.** Web Service, runtime Docker, free instance type, health check path `/health`, auto-deploy from the
deployment branch. In Atlas, Network Access must allow the host's addresses; the free tier has no fixed addresses, so it
needs `0.0.0.0/0`.

**Single instance only.** Live updates use SignalR groups held in memory and the NPC turn runner lives inside the web
process, so a second instance would split viewers and could play the same NPC turn twice. Do not scale out without
adding a SignalR backplane.

**Spin-down.** On Render's free tier the service sleeps after 15 minutes without traffic and takes about a minute to wake.
NPCs do not play while it sleeps; unfinished games are picked up again when it wakes. An external pinger on
`/health` (for example UptimeRobot) keeps it awake.

**Health check.** `GET /health` returns 200 when MongoDB answers a ping and 503 when it does not.

## Credentials

_To be completed._
