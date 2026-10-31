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

- **Built from facts only.** The prompt is made from `BattleContext`: catalogue country names, soldiers before and
  after, dice and round numbers. Player names are not in it, so nothing a user typed reaches the model.
- **Never slows play.** A roll is saved and returned straight away, and commentary is written by a background worker
  within a 2.5 second budget. It goes into a log position reserved with the roll, so it needs no second save of the
  game, cannot conflict with the next move, and reads in the right order however late it arrives.
- **Always available.** `ResilientCommentaryService` falls back to `TemplateCommentaryService` on a timeout, a 429, an
  error or a missing key. It stays within a per-minute limit and honours `retry-after`. Fast-forwarded NPC rolls use
  the template without spending a request.
- **Output is untrusted.** `CommentaryOutputGuard` cuts text back to two whole sentences and 240 characters, and
  rejects anything empty, off-topic or unsuitable for a family audience. The board only inserts it as plain text.
- **Persisted and labelled.** Commentary is a `Commentary` event, so spectators and reloads see it, and each line says
  whether the model or a template wrote it.

The API key comes from `dotnet user-secrets` (`Groq:ApiKey`) or the `Groq__ApiKey` environment variable. It is never
logged: the HTTP client's request logging is removed, and the service logs status codes only.

## Security

_To be completed._

## Deployment

_To be completed._

## Credentials

_To be completed._
