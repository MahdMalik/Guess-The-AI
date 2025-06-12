'use client'
import Image from "next/image";
import { Button } from "@mui/material";
import axios from "axios";

export default function Home() {
    
    const joinQueue = async() => {
        const response = await axios.get("http://localhost:5017/join_queue")
        const data = response.data
        if(data.success)
        {
            console.log(data.message)
        }
        else
        {
            console.log("Communication failed!")
        }
    }
    
    return (
        <div>
            <Button variant="contained" onClick={testNetCall}>Click Here To Get Added to the Queue!</Button>
        </div>
    )
}