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
    const [mode, setMode] = useState("Discussion")

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
        else if (data.message == "Voting Time")
        {
            console.log("Time to vote!")
            setMode("Voting")
            const packet = {
                username: username,
                server_id: server_id,
                messageType: "Add Vote",
                votedPerson: username
            }
            socket.current.SendData(packet)
        }
        else if(data.message == "Discussion Time")
        {
            console.log("Discuss time again! The voted player was: " + data.voted_person)
            setMode("Discussion")
        }
        else if(data.message == "Voted Out")
        {
            alert("LMAO YOU GOT VOTED OUT BUM!")
            socket.current.socket.removeEventListener("message", socket.current.MessageListener)
            socket.current.socket.close(1000, "Done")
            window.location.href = "/queue"
        }
        else if(data.message == "Game Over")
        {
            alert("Game is over now! Winner: " + data.winner + "! Oh yeah last person voted out was: " + data.voted_person)
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
                {/* This way if it's in discussion, it'll provide the button to send another message */}
                {mode == "Discussion" && <Button onClick={SendNewMessage}>Send A Message</Button>}
                {messages.map((message, index) => 
                    (<p key={index}>Message #{index + 1} by {message.name}: {message.message}</p>)
                )}
                {mode == "Voting" && <p>Voting now! You'll be voting yourself though lol.</p>}
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