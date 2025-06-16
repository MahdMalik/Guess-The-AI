'use client'
import Image from "next/image";
import { Button } from "@mui/material";

export default function Home() {
  
  const sendToQueue = async() => {
    window.location.href = "/queue"
  }
  
  return (
    <div>
      <Button variant="contained" onClick={sendToQueue} >Click to get to queue page!</Button>
    </div>
  );
}
