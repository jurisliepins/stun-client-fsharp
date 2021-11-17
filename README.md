# STUN client

F# implementation of a [STUN](https://en.wikipedia.org/wiki/STUN) client.

Client was implemented as an exercise, following the deprecated [RFC3489](https://datatracker.ietf.org/doc/html/rfc3489) and is not meant to be used in production. 

## Usage

### Building

From project root run `make`. Alternatively you can build the project yourself from cmd (see Makefile for examples commends) or from an IDE.

Makefile is configured to compile a self contained single binary.

### Running

From project root run `.\src\STUN.Client.FSharp\bin\Release\net6.0\win-x64\stun-client-fsharp.exe -s stun.l.google.com:19302` to run the client against a google STUN server. A linux-x64 binary will also be compiled in the linux-64 folder.