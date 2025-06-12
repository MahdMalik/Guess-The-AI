'use client'
import Image from "next/image";
import { Button } from "@mui/material";
import axios from "axios";
import { useEffect, useReducer, useState, useRef } from "react";

export default function Home() {
    const [inQueue, setQueueStatus] = useState(false);
    const [username, setName] = useState("walter")
    const socket = useRef(null);

    const ConnectToServer = () => {
        return new Promise ((resolve, reject) => {
            const socket = new WebSocket("ws://localhost:5017/ws")
            socket.onopen = () => {
                resolve(socket)
            }

            socket.onerror = (err) => {
                reject(err)
            }

        })
    }

    const toggleQueue = async() => {
        //means there's already a connection
        if(socket.current != null)
        {
            socket.current.send(JSON.stringify({
                username: username,
                messageType: "Leave Queue",
                queueType: "One Bot Game"
            }))
            setQueueStatus(false);
        }
        else
        {
            try
            {
                socket.current = await ConnectToServer();
                console.log("Socket connected!");
                socket.current.send(JSON.stringify({
                    username: username,
                    messageType: "Join Queue",
                    queueType: "One Bot Game"
                }))
                setQueueStatus(true)
                socket.current.onerror = (error) => {
                    alert("Error with socket: " + error)
                }
                socket.current.onclose = () => {
                    console.log("Socket conneciton closed!")
                    setQueueStatus(false);
                    socket.current = null;
                }
            }
            catch (e)
            {
                alert("Unable to make connection: " + e)
            }

        }
    }
    
    return (
        <div>
            <Button variant="contained" onClick={toggleQueue}>Click Here To Get 
                {
                    (inQueue) ? "Removed from" : "Added to"
                } 
                the Queue!
            </Button>
        </div>
    )
}