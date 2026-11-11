# Todo Prior to running:
# - install python in VS Code (python + pip)
# - run 'pip install agent-framework --pre' (takes a suprisingly long time)

import os
import asyncio
from agent_framework.azure import AzureOpenAIResponsesClient

#Needed for structured Output
from typing import List
from pydantic import BaseModel

os.system("cls") #Clear the screeen

def get_client() -> AzureOpenAIResponsesClient :
    return AzureOpenAIResponsesClient(
        endpoint="https://sensum365ai.openai.azure.com/",
        #api_key=os.environ["AZURE_OPENAI_API_KEY"],
        deployment_name="gpt-6-luna",
    )

async def chat():
    client = get_client()
    agent = client.create_agent(instructions="You are a nice AI")
    myThread = agent.get_new_thread()
    while(True) :
        myInput = input("> ")
        response = await agent.run(myInput, thread=myThread)
        print(response)

async def chat_streaming():
    client = get_client()
    agent = client.create_agent(instructions="You are a nice AI")
    myThread = agent.get_new_thread()
    while(True) :
        myInput = input("> ")
        async for chunk in agent.run_stream(myInput):
            if chunk.text:
                print(chunk.text, end="", flush=True)
        print()

def get_weather_tool(city: str) -> str:
    print(f"\033[90m--Calling tool with city={city}--\033[0m",)
    return f"Weather in {city} is Sunny and 19 degrees"

async def chat_with_tool():
    client = get_client()
    agent = client.create_agent(
        instructions="You are a nice AI",
        tools= [ get_weather_tool ] #Middleware is possible, but a bit more clunky than C#
    )
    myThread = agent.get_new_thread()
    while(True) :
        myInput = input("> ")
        response = await agent.run(myInput, thread=myThread)
        print(response)

class Movie(BaseModel):
    title: str
    year: int
    director: str

class MovieCollection(BaseModel):
    movies: List[Movie]

async def chat_with_structrured_output():
    client = get_client()
    agent = client.create_agent(
        instructions="You are a nice AI",
        response_format=MovieCollection
    )
    myThread = agent.get_new_thread()
    while(True) :
        myInput = input("> ")
        response = await agent.run(myInput, thread=myThread)
        print(response)

#asyncio.run(chat())
#asyncio.run(chat_streaming())
#asyncio.run(chat_with_tool())
asyncio.run(chat_with_structrured_output())