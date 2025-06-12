'use client'
import Image from "next/image";
import { Button } from "@mui/material";
import axios from "axios";
import { useEffect, useReducer, useState, useRef } from "react";

export default function Home() {
    const [inQueue, setQueueStatus] = useState(false);
    const [username, setName] = useState(navigator.userAgent)
    const socket = useRef(null);

    //helper function to await getting am essage from the server through promises
    const ReceiveMessage = () => {
        return new Promise((resolve, reject) => {
            socket.current.onmessage = (response) => {
                const data = JSON.parse(response.data)
                resolve(data)
            }
            socket.current.onerror = (err) => {
                reject(err)
            }
        })
    }

    //helper function to await connection to a server through promises
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
            //send message to leave
            socket.current.send(JSON.stringify({
                username: username,
                messageType: "Leave Queue",
                queueType: "One Bot Game"
            }))
            //receive response, make sure we were actually able to leave
            try
            {
                const data = await ReceiveMessage();
                console.log(data)
                //if succeeded in removal, set them no longer in the queue, close socket, reset ref
                if(data.success)
                {
                    setQueueStatus(false)
                    socket.current.close()
                    socket.current = null
                }
                else
                {
                    alert("Something failed while attempting to leave queue: " + data.message)
                }
            }
            catch(e)
            {
                alert("Error trying to receive mesasge")
            }
        }
        //means have to start connection
        else
        {
            //try to connect
            try
            {
                socket.current = await ConnectToServer();
                console.log("Socket connected!");
                socket.current.send(JSON.stringify({
                    username: username,
                    messageType: "Join Queue",
                    queueType: "One Bot Game"
                }))
                const data = await ReceiveMessage();
                console.log(data)
                //if able to connect, put on these listeners
                if(data.success)
                {
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
                else
                {
                    setQueueStatus(false)
                    socket.current.close()
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
                    (inQueue) ? " Removed from " : " Added to "
                } 
                the Queue!
            </Button>
        </div>
    )
}