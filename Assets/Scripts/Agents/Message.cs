using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Message {

	protected string performative;
	protected string content; 
	protected string sender;
	protected string receiver;

	public Message(string performative, string content, string sender, string receiver){
		if (performative == null)
		{
			throw new Exception("Performative parameter can´t be null");
		}
		this.performative = performative;

		if (content == null)
		{
			throw new Exception("Content parameter can´t be null");
		}
		this.content = content;

		if (sender ==null)
		{
			throw new Exception("Sender parameter can´t be null"); 
		}
		this.sender = sender;

		if (receiver ==null)
		{
			throw new Exception("Receiver parameter can´t be null"); 
		}	
		this.receiver = receiver;		
	}
    //Getters
	public string GetPerformative() {
		return this.performative;
	}
	public string GetContent() {
		return this.content;
	}
	public string GetSender() {
		return this.sender;
	}
	public string GetReceiver() {
		return this.receiver;
	}
}