'use client'
import { useEffect, useState, useRef } from "react";
import { Sockets } from "../components/sockets";

export default function Home() {
    const socket = useRef(null);
    const [username, setName] = useState(navigator.userAgent)
    const [server_id, setId] = useState("")

    const OnMessageFunction = async (data) => {
        
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
    
    return (<div>
        <p>Skibidi!</p>
    </div>)
}