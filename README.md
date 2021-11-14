# STUN client

F# implementation of a [STUN](https://en.wikipedia.org/wiki/STUN) client.

Client was implemented as an exercise, following the deprecated [RFC3489](https://datatracker.ietf.org/doc/html/rfc3489) and is not meant to be used in production. 

## Building

From project root run `make`.

## Running

On windows, from project root run `.\src\STUN.Client.FSharp\bin\Release\net6.0\win-x64\stun-client-fsharp.exe -s stun.l.google.com:19302` passing in the server endpoint.

On linux, from project root run `.\src\STUN.Client.FSharp\bin\Release\net6.0\linux-x64\stun-client-fsharp -s stun.l.google.com:19302` passing in the server endpoint.