export class Sockets
{
    constructor(username, OnMessageFunction, server_id, connection_type)
    {
        this.socket = null;
        this.status = "Closed"
        this.username = username
        this.MessageListener = null
        this.OnMessageFunction = OnMessageFunction
        this.server_id = server_id
        this.connection_type = connection_type
    }

    //helper function to await getting am essage from the server through promises
    RetrieveConfirmationMessage = () => {
        return new Promise((resolve, reject) => 
        {
            const getConfirm = (response) => {
                const data = JSON.parse(response.data)
                if(data.message == "Confirmation")
                {
                    this.socket.removeEventListener("message", getConfirm)
                    resolve(data)
                }
            }
            this.socket.onerror = (err) => {
                reject(err)
            }

            this.socket.addEventListener("message", getConfirm)
        })
    }

    async ListenForMessages()
    {
        this.MessageListener = (response) => {
            console.log(response)
            const data = JSON.parse(response.data)
            if(data.message != "Confirmation" && data.success)
            {
                //do something with it
                this.OnMessageFunction(data)
            }
        }
        this.socket.addEventListener("message", this.MessageListener)
    }

    async SendData(obj)
    {
        //if we got the server id, want to send it as well
        if(this.server_id != null)
        {
            obj = {...obj, server_id: this.server_id}
        }
        this.socket.send(JSON.stringify(obj))
        const data = await this.RetrieveConfirmationMessage()
        console.log("Confirm received!")
        return data
    }

    //method to actually create the socket, since constructors can't be async
    async CreateSocket(messageType, queueType) {
        try
        {
            this.socket = await Sockets.ConnectToServer();
            console.log("Socket connected!");
            const obj = {
                username: this.username,
                messageType: messageType,
                queueType: queueType
            }
            const result = await this.SendData(obj)
            this.ListenForMessages();
            return result
        }
        catch(e)
        {
            alert("Unable to make connection: " + e)
            return {result: false, packet: null}
        }

    }

    //helper function to await connection to a server through promises
    static async ConnectToServer() {
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

    async CloseSocket()
    {
        this.socket.removeEventListener("message", socket.current.MessageListener)
        this.socket.close(1000, "Close Ok")
    }
}