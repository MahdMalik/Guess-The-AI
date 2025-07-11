'use client'
import { useEffect, useState, useRef } from "react";
import { Sockets } from "../components/sockets";
import { Button, Stack, Box, Drawer, List, ListItem, ListItemText } from "@mui/material";

export default function Home() {
    const socket = useRef(null);
    const [username, setName] = useState(navigator.userAgent)
    const [server_id, setId] = useState("")
    const [gameStarted, setStart] = useState(false)
    const [messages, setMessages] = useState([])
    const [mode, setMode] = useState("Discussion")
    const [players, setPlayers] = useState([])
    const [hasVoted, setHasVoted] = useState(false) 

    const VoteOutPlayer = (name) => {
        const index = players.indexOf(name)
        setPlayers((prev) => {
            return prev.splice(index, 1)
        })
    }

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
            setHasVoted(false)
        }
        else if(data.message == "Discussion Time")
        {
            console.log("Discuss time again! The voted player was: " + data.voted_person)
            VoteOutPlayer(data.voted_person)
            setMode("Discussion")
        }
        else if(data.message == "Voted Out")
        {
            alert("LMAO YOU GOT VOTED OUT BUM!")
            socket.current.socket.removeEventListener("message", socket.current.MessageListener)
            socket.current.socket.close(1000, "Finished Match")
            window.location.href = "/queue"
        }
        else if(data.message == "Game Over")
        {
            VoteOutPlayer(data.voted_person)
            alert("Game is over now! Winner: " + data.winner + "! Oh yeah last person voted out was: " + data.voted_person)
            socket.current.socket.removeEventListener("message", socket.current.MessageListener)
            socket.current.socket.close(1000, "Finished Match")
            window.location.href = "/queue"
        }
    }

    useEffect(() => {
        
        const startFunct = async () => {
            const hashId = sessionStorage.getItem("server_id")
            setId(hashId)
            //now, reset it back to what it once was    
            sessionStorage.setItem("server_id", "")
            socket.current = new Sockets(username, OnMessageFunction, hashId, "Match")
            const result = await socket.current.CreateSocket("Join Match", "One Bot Game")
            //if there's something to parse...
            if(result.success)
            {
                console.log("Yippee!")
                //should have sent the player list along here too:
                setPlayers(result.names)
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

    const SendVote = async(player) => {
        const packet = {
            username: username,
            server_id: server_id,
            messageType: "Add Vote",
            votedPerson: player
        }
        socket.current.SendData(packet)
        setHasVoted(true)
    }
    
    return (<div>
        { gameStarted ? 
        (
            <Box display="flex" alignItems = "flex-start">
                <Box flex={1} p ={2}>
                    <p>Game Start!</p>
                    {/* This way if it's in discussion, it'll provide the button to send another message */}
                    {mode == "Discussion" && <Button onClick={SendNewMessage}>Send A Message</Button>}
                    {messages.map((message, index) => 
                        (<p key={index}>Message #{index + 1} by {message.name}: {message.message}</p>)
                    )}
                    {mode == "Voting" && <p>Voting now! Pick who you want from the sidebar.</p>}
                </Box>
                <Drawer
                    variant="permanent"
                    anchor="right"
                    sx={{
                    width: 240,
                    flexShrink: 0,
                    [`& .MuiDrawer-paper`]: { width: 240, boxSizing: 'border-box' },
                    }}
                >
                    <p>Players:</p>
                    {players.map((player, i) =>
                        (<Stack flexDirection="row" key={i}>
                            <p>#{i+1}: {player}</p>
                            {mode == "Voting" && !hasVoted && <Button variant="contained" onClick={() => SendVote(player)}>VOTE</Button>}
                        </Stack>)
                    )}
                </Drawer>
            </Box>
           
        ) : 
        (
            <div>
                <p>Waiting for players....</p>
            </div>
        )
        }
    </div>)
}