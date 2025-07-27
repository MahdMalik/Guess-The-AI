'use client'
import { useEffect, useState, useRef } from "react";
import { Sockets } from "../components/sockets";
import { Button, Stack, Box, Drawer, List, ListItem, ListItemText, TextField } from "@mui/material";
import {
  Chart as ChartJS,
  BarElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Legend,
  BarController
} from 'chart.js';

ChartJS.register(BarElement, CategoryScale, LinearScale, BarController, Tooltip, Legend);

import { Chart } from "react-chartjs-2";

export default function Home() {
    const socket = useRef(null);
    const [username, setName] = useState(navigator.userAgent)
    const [server_id, setId] = useState("")
    const [gameStarted, setStart] = useState(false)
    const [messages, setMessages] = useState([])
    const [mode, setMode] = useState("Discussion")
    const [players, setPlayers] = useState([])
    const [hasVoted, setHasVoted] = useState(false) 
    const [message, setMessage] = useState("")
    const [votedPerson, setVotedPerson] = useState("")
    const [fairVote, setIfFairVote] = useState(true)
    
    const [graphData, setGraphData] = useState({
        labels: [],
        datasets: [
            {
                label: 'Votes',
                data: [],
                backgroundColor: []
            }
        ]
    })

    const graphOptions = 
    {
        scales: 
        {
            x: {
                ticks: 
                {
                    maxRotation: 0,
                    minRotation: 0,
                }
            },
            y: 
            {
                beginAtZero: true,
                ticks: 
                {
                    stepSize: 1,  // <-- increments of 1
                }
            }
        }
    };

    //using the player name, removes them from the array of players
    const VoteOutPlayer = (name, fair) => {
        const index = players.indexOf(name)
        setPlayers((prev) => {
            return prev.splice(index, 1)
        })
        //also want to send a system message that they were voted out
        let message = name + " WAS VOTED OUT"
        if(fair)
        {
            message += "!"
        }
        else
        {
            message += " RANDOMLY DUE TO TIE!"
        }
        setMessages(prev => [...prev, {name: "SYSTEM", message: message}])
    }

    //gets a random color for all the players
    const GetColors  = (numPlayers) => {
        const colorArr = new Array(numPlayers)
        //loops through all the players
        for (let i = 0; i < numPlayers; i++ )
        {
            //using hex format
            const possibleColors = "0123456789ABCDEF"

            let element = "#"
            //6 possible numbers for each color
            for(let j = 0; j < 6; j++)
            {
                element+= possibleColors[Math.floor(Math.random() * possibleColors.length)]
            }
            colorArr[i] = element
        }
        return colorArr;
    }

    //sets the graph with our votes and knowing the players voted
    const SetGraph = (votes, numberOfPlayersVoted) => {
        //initialzie the array at the beginning so we're not  constantly creating a new array with one extra element
        const barLabels = new Array(numberOfPlayersVoted)
        const dataPoints = new Array(numberOfPlayersVoted);
        let playerNumber = 0;
        //traverse outer array, then inner array
        for(let i = 0; i < votes.length; i++)
        {
            for(const name of votes[i])
            {
                barLabels[playerNumber] = name;
                //remember, index 0 means vote of 1
                dataPoints[playerNumber] = i + 1
                playerNumber++;
            }
        }
        const barColors = GetColors(numberOfPlayersVoted)

        //update the graph
        setGraphData(prev => {
            return {labels: barLabels, datasets: [{
                label: prev.datasets[0].label,
                data: dataPoints,
                backgroundColor: barColors
            }]}
        })
    }

    //the function called whenever we receive a message from the server. Most are self explanatory
    const OnMessageFunction = async (data) => {
        //GAME START!
        console.log(data)
        switch(data.message)
        {
            case "Game Start! Discussion First":
                setStart(true)    
                break;
            case "Message Arrived":
                console.log("Message added!")
                setMessages((prev) => [...prev, {name: data.sender, message: data.sentMessage}])
                break;
            case "Voting Time":
                console.log("Time to vote!")
                setMode("Voting")
                setHasVoted(false)
                break;
            case "Person Voted Out":
                setVotedPerson(data.voted_person)
                setIfFairVote(data.fair_voted_out)

                VoteOutPlayer(data.voted_person, data.fair_voted_out)
                SetGraph(data.votes, data.num_voted)
                setMode("Intermission")
                break;
            case "Discussion Time":
                setMode("Discussion")
                break;
            case "Voted Out":
                if(data.fair_voted_out)
                {
                    alert("LMAO YOU GOT VOTED OUT BUM!")
                }
                else
                {
                    alert("You were picked to be randomly eliminated due to a tie! Sorry")
                }
                socket.current.CloseSocket("Game Over");
                window.location.href = "/queue"
                break;
            case "Game Over":
                alert("Game is over now! Winner: " + data.winner + "!")
                socket.current.CloseSocket("Game Over");
                window.location.href = "/queue"
                break;
            default:
                break;
        }
    }

    //connects to the server on startup
    useEffect(() => {
        //to use async in a useeffect, you have to assign it to a function and then call that function
        const startFunct = async () => {
            //last time as we went to the match, we stored the id in localstorage, now we're getting that
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

    //allows for sending a new message to the server
    const SendNewMessage = async() => {
        //don't want to just allow sending blank messages
        if(message.trim() == "")
        {
            return
        }
        const packet = {
            username: username,
            server_id: server_id,
            messageType: "New Message",
            saidMessage: message
        }
        setMessage("")
        await socket.current.SendData(packet)
    }

    //lets you send a new vote to the server
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
                    {messages.map((message, index) => 
                        (<p key={index}>Message #{index + 1} by {message.name}: {message.message}</p>)
                    )}

                    {/* This way if it's in discussion, it'll provide the button to send another message */}
                    {mode == "Discussion" && 
                        <Stack flexDirection="row">
                            <TextField placeholder="Message Here" value={message} onChange={(e) => setMessage(e.target.value)} onKeyDown={(e) => {
                                if(e.key == "Enter")
                                {
                                    SendNewMessage();
                                }
                            }}/>
                            <Button onClick={SendNewMessage}>Send</Button>
                        </Stack>
                    }

                    {mode == "Voting" && <p>Voting now! Pick who you want from the sidebar.</p>}
                    {mode == "Intermission" && <Chart type="bar" data={graphData} options={graphOptions}/>}
                </Box>
                {/* Here is where the sidebar is drawn. */}
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