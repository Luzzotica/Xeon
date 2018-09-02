using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour 
{

    public GameObject client;

    public GameObject chatPanel, textPrefab;
    public InputField chatBox;

    public Color playerMessage, info, alert, serverMessage;

    private int maxMessages = 50;

    List<Message> messages = new List<Message>();
	
	// Update is called once per frame
	void Update ()
    {
        // If the chat box is not empty
        if (chatBox.text != "")
        {
            // And we hit enter
            if (Input.GetKeyDown(KeyCode.Return))
            {
                // Send the message!
                SendMessageToChat(chatBox.text, Message.MessageType.playerMessage);

                chatBox.text = "";

                chatBox.DeactivateInputField();
            }
        }
        // If the chat box was empty and we hit return, return focus to the game
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            chatBox.DeactivateInputField();
        }
        // If the chat box was not focused and we hit enter, put focus on the chat box
        else if (!chatBox.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            chatBox.ActivateInputField();
        }
	}

    public void SendMessageToChat(string text, Message.MessageType messageType)
    {
        // If we have exceeded max messages, delete the last one
        if (messages.Count > maxMessages)
        {
            // Destroy the text object
            Destroy(messages[0].textObject.gameObject);
            // Remove the message from the list
            messages.Remove(messages[0]);
        }

        // Create a new message
        Message message = new Message();
        // Set its text
        message.text = text;
        // Create a new text object from our prefab
        GameObject newText = Instantiate(textPrefab, chatPanel.transform);
        // Set the text component of the instatiated object
        message.textObject = newText.GetComponent<Text>();

        // Set the text and color of the text
        message.textObject.text = message.text;
        message.textObject.color = MessageTypeColor(messageType);

        // Add the message to the message list
        messages.Add(message);

    }

    public void SendDeathMessageToChat(string playerName, string targetName)
    {
        // Get a random death message
        string deathMessage = GetRandomDeathMessage();

        // Replace the @p and @t with the pname and target name
        deathMessage = deathMessage.Replace("@p", playerName);
        deathMessage = deathMessage.Replace("@t", targetName);

        // Send the message to the chat
        SendMessageToChat(deathMessage, Message.MessageType.serverMessage);
    }

    Color MessageTypeColor(Message.MessageType type)
    {
        switch (type)
        {
            case Message.MessageType.playerMessage:
                return playerMessage;
            case Message.MessageType.serverMessage:
                return serverMessage;
            case Message.MessageType.info:
                return info;
            case Message.MessageType.alert:
                return alert;
            default:
                return playerMessage;
        }
    }

    string GetRandomDeathMessage()
    {
        float rand = Random.value;
        if (rand < 0.1) { return "@p violently murdered @t"; }
        else if (rand < 0.2) { return "@p murdered @t"; }
        else if (rand < 0.3) { return "@p assassinated @t"; }
        else if (rand < 0.4) { return "@p destroyed @t"; }
        else if (rand < 0.5) { return "@p shot @t in the face"; }
        else if (rand < 0.6) { return "@t got a little too close to @p"; }
        else if (rand < 0.7) { return "@t got comfy with @p, then died"; }
        else if (rand < 0.8) { return "@t has forcibly retired his body! Thanks @p!"; }
        else if (rand < 0.9) { return "@p played the death card on @t"; }
        else if (rand < 1.0) { return "@p danced @t to his (or her) grave"; }
        else return " killed ";
    }
}

public class Message
{
    public string text;
    public Text textObject;

    public enum MessageType
    {
        playerMessage, info, alert, serverMessage
    }
}
