# Create MRN Real-Time News Story Consumer app using .NET Core and LD.NET Library

## Introduction

It's been a while we receive a question from the .NET developer about easy to use solutions that can help them save the time to develop the application for retrieving market data from the data feed via both the LSEG Real-Time Distribution System and Real-Time – Optimized.

Typically,  Real-Time – Optimized and Real-Time Distribution System version 3.2.x and later version provide a Websocket connection for a developer so that they can use any WebSocket client library to establish a connection and communicate with the WebSocket server directly. Anyway, to retrieve data from Real-Time – Optimized, it requires additional steps that the connecting user must authenticate themselves before a session establishing with the server. Moreover, the application also needs to design the application's workflow to control the JSON request and response message along with maintaining the ping and pong, which is a heartbeat message between the client app and the server-side. As a result, the workflow to create the application is quite a complicated process.

The LD.NET are ease-of-use APIs defining a set of uniform interfaces providing the developer access to the Data Platform and Real-Time – Optimized. The APIs are designed to provide consistent access through multiple access channels; developers can choose to access content from the desktop, through their deployed streaming services or directly to the cloud. The interfaces encompass a set of unified Web APIs providing access to both streaming (over WebSockets) and non-streaming (HTTP REST) data available within the platform.

This article will provide a sample usage to create a .NET Core console app to retrieve MRN News Story from the infra using the LSEG Data Library for .NET. It will describe how to use an interface from Core/Delivery Layer, to request a Streaming data, especially real-time News from MRN Story data. This article also provides an example application that implements a function to manage a JSON request and response message. It will show you how to concatenate and decompress MRN data fragments manually. Moreover, the article will show you alternate options from a Content Layer to retrieve the same MRN Story data.

For the full article, please visit [this link](https://developers.lseg.com/en/article-catalog/article/create-mrn-real-time-news-story-consumer-app-using-net-core-and-ldnet-library).
