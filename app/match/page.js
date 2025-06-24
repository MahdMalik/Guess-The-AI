'use client'
import { useEffect, useState, useRef } from "react";
import { Sockets } from "../components/sockets";
import { Button, Stack } from "@mui/material";

export default function Home() {
    const socket = useRef(null);
    const [username, setName] = useState(navigator.userAgent)
    const [server_id, setId] = useState("")
    const [gameStarted, setStart] = useState(false)
    const [messages, setMessages] = useState([])

    const OnMessageFunction = async (data) => {
        //GAME START!
        console.log(data)
        if(data.message == "Game Start! Discussion First")
        {
            setStart(true)
        }
        else if(data.type != null && data.type == "Message Arrived")
        {
            console.log("Message added!")
            setMessages((prev) => [...prev, {name: data.sender, message: data.message}])
        }
    }

    useEffect(() => {
        
        const startFunct = async () => {
            const hashId = sessionStorage.getItem("server_id")
            setId(hashId)
            //now, reset it back to what it once was    
            sessionStorage.setItem("server_id", "")
            socket.current = new Sockets(username, OnMessageFunction, hashId, "Match")
            const success = await socket.current.CreateSocket("Join Match", "One Bot Game")
            if(success)
            {
                console.log("Yippee!")
            }
            else
            {
                socket.current = null
                alert("HEY HEY HEY WE FAILD WE FAILED!!")
            }
        }
        startFunct()
    }, [])

    const SendNewMessage = async() => {
        const packet = {
            username: username,
            server_id: server_id,
            messageType: "New Message",
            saidMessage: "Hello!"
        }
        await socket.current.SendData(packet)
    }
    
    return (<div>
        { gameStarted ? 
        (
            <div>
                <p>Game Start!</p>
                <Button onClick={SendNewMessage}>Send A Message</Button>
                {messages.map((message, index) => 
                    (<p key={index}>Message #{index + 1} by {message.name}: {message.message}</p>)
                )}
            </div>
           
        ) : 
        (
            <div>
                <p>Waiting for players....</p>
            </div>
        )
        }
    </div>)
}