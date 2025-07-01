'use client'
import Image from "next/image";
import { Button } from "@mui/material";
import axios from "axios";
import { useEffect, useReducer, useState, useRef } from "react";
import { Sockets } from "../components/sockets";
export default function Home() {
    const [inQueue, setQueueStatus] = useState(false);
    const [username, setName] = useState(navigator.userAgent)
    const socket = useRef(null);

    const OnMessageFunction = async (data) => {
        if(data.message == "Game Starting!")
        {
            alert("GAME STARTING!!")
            socket.current.socket.removeEventListener("message", socket.current.MessageListener)
            sessionStorage.setItem("server_id", data.server_id)
            window.location.href = "/match"
        }
    }

    const toggleQueue = async() => {
        //means there's already a connection
        if(socket.current != null)
        {
            const leaveResponse = {
                messageType: "Leave Queue",
                username: username,
                queueType: "One Bot Game"
            }
            const response = await socket.current.SendData(leaveResponse)
            if(!response.success)
            {
                console.log("Error doing that: " + response.message)
            }
            else
            {
                socket.current.socket.close()
                socket.current = null
                setQueueStatus(false)
                console.log("Closed Connection!")
            }
        }
        //means have to start connection
        else
        {
            socket.current = new Sockets(username, OnMessageFunction, null, "Queue")
            const result = await socket.current.CreateSocket("Join Queue", "One Bot Game")
            if(result.success)
            {
                setQueueStatus(true)
            }
            else
            {
                setQueueStatus(false)
                socket.current = null
            }
        }
    }
    
    return (
        <div>
            <Button variant="contained" onClick={toggleQueue}>Click Here To Get
                {
                    (inQueue) ? " Removed from " : " Added to "
                } 
                the Queue!
            </Button>
        </div>
    )
}