////////////////////////////////////////////////////////////////////////
// Loader.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace UnityJS {


public class Loader : MonoBehaviour {


	static public Loader loader;

	protected ArrayList coroutineQueue = new ArrayList();
	protected ArrayList coroutineRunList = new ArrayList();

	public int coroutineRunMax = 5;


	void Start()
	{
		//Debug.Log("START LOADER " + this);
		loader = this;
	}


	// Update is called once per frame
	void FixedUpdate()
	{
		//Debug.Log("********** Loader Update coroutineQueue " + coroutineQueue.Count + " coroutineRunList " + coroutineRunList.Count);
		RunCoroutineQueue();
	}


	public void RunCoroutineQueue()
	{
		//Debug.Log("==== RunCoroutineQueue coroutineQueue " + coroutineQueue.Count + " coroutineRunList " + coroutineRunList.Count);
		if (ShouldDelayCoroutine()) {
			//Debug.Log("RunCoroutineQueue should delay");
			return;
		}
	
		if (coroutineQueue.Count == 0) {
			//Debug.Log("RunCoroutineQueue none queued");
			return;
		}

		string url =
			(string)coroutineQueue[0];
		IEnumerator coroutine =
			(IEnumerator)coroutineQueue[1];
		coroutineQueue.RemoveAt(0);
		coroutineQueue.RemoveAt(0);

		coroutineRunList.Add(url);
		coroutineRunList.Add(coroutine);

		//Debug.Log("!!!!!! Starting coroutine " + url);

		StartCoroutine(coroutine);
	}


	public bool ShouldDelayCoroutine()
	{
		return coroutineRunList.Count >= (coroutineRunMax * 2);
	}


	public bool ShouldDelayPriorityCoroutine(int priority)
	{
		return coroutineRunList.Count > priority;
	}


	public void QueueCoroutine(string url, IEnumerator coroutine)
	{
		//Debug.Log("Queuing coroutine " + url + " " + coroutine);

		if (coroutineQueue.Contains(url)) {
			Debug.Log("!!!!!! QueueCoroutine duplicate URL " + url);
		}

		coroutineQueue.Add(url);
		coroutineQueue.Add(coroutine);
		//Debug.Log("now coroutineQueue " + coroutineQueue.Count);
	}


	public void DequeueCoroutine(string url)
	{
		//Debug.Log("Dequeuing coroutine " + url);

		int i = coroutineRunList.IndexOf(url);
		if (i == -1) {
			Debug.Log("DequeueCoroutine missing URL " + url);
			return;
		}

		coroutineRunList.RemoveAt(i);
		coroutineRunList.RemoveAt(i);
		//Debug.Log("coroutineRunList Count " + coroutineRunList.Count);
	}


}


}
