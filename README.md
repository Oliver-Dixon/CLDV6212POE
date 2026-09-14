This is my Part 1 project for CLDV6212. It is a cloud system for a business called CoffeeNChill. I built a system that stores all of that in cloud storage instead.

What I built in VS Code

I used VS Code with C# and .NET 8. The project is an Azure Functions app which means each part of the API is its own small function instead of one big program. I split the code into three folders. The Models folder holds the shapes of the data. The Services folder holds all the code that talks to storage. The Functions folder holds the web endpoints. I did this so the storage code stays separate from the web code which makes it easier to change later.

Menu and tables

The menu items are saved in Azure Table Storage in a table called MenuItems. Table Storage is not like a normal SQL database. Every row needs two keys. I used the category as the PartitionKey and the item SKU as the RowKey. I chose the category because then asking for all the hot drinks only looks inside that one group which is much faster.

I built five endpoints for the menu. You can create a new item, get every item, get items from one category, update the price or availability of an item, and delete an item.

I also added checks on the data. If you send a bad SKU or a price below zero the system says no and tells you what was wrong. If you ask for something that does not exist you get a not found message. If you try to add the same item twice you get a conflict message.

Documents

Staff documents like recipe sheets and cleaning manuals are stored in a place called staff-docs. I built three endpoints. One uploads a file, one lists every file with its name and size and date, and one downloads a file back.

Only PDF, PNG, JPEG and Word files are allowed. If you send anything else it gets rejected. Files are streamed through so a big file does not fill up the memory.

One important note. The brief asked for an Azure File Share but Azurite does not support file shares at all. It only supports blobs, queues and tables. So I used blob storage instead and put it behind an interface called IDocumentService. This means if a real file share is ever needed I only have to write one new class and change one setting. Nothing else has to change.

Docker

I wrote a Dockerfile that builds the app into a container image. It uses two stages. The first stage builds the code. The second stage only copies the finished build into a smaller image. This keeps the final image much smaller because the build tools are not needed when it is running.

There are two containers. One is Azurite which pretends to be Azure Storage on my own machine. The other is the Functions app. There is no Docker Compose. Each container is started on its own.

Postman

I made a Postman collection with 18 requests split into two folders. One folder is for the menu and one is for the documents. Every request uses a variable called baseUrl instead of typing the address each time. Every request also has tests that check the status code and the response. I tested the good cases and the bad cases so the error handling is proven to work. The collection and the environment file are both in the docs folder.

Docker Hub links

https://hub.docker.com/r/st10438499/coffeenchill-functions 
https://hub.docker.com/r/st10438499/coffeechill-azurite
