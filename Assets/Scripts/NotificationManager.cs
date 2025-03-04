using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class Notification
{
    public string message;
    public Color color;

    public Notification(string message, Color color)
    {
        this.message = message;
        this.color = color;
    }
}


public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("Notification UI")]
    public TMP_Text notificationText;
    public Image notificationBackground;
    public float notificationDuration = 2f;

    private Queue<Notification> notificationQueue = new Queue<Notification>();
    private bool isDisplaying = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void QueueNotification(string message, Color color)
    {
        Notification notification = new Notification(message, color);
        notificationQueue.Enqueue(notification);
        if (!isDisplaying)
        {
            StartCoroutine(DisplayNotifications());
        }
    }

    private IEnumerator DisplayNotifications()
    {
        isDisplaying = true;
        while (notificationQueue.Count > 0)
        {
            Notification notification = notificationQueue.Dequeue();
            notificationText.text = notification.message;
            Color notificationColor = notification.color;
            notificationColor.a = 0.3f;
            notificationBackground.color = notificationColor;
            notificationText.gameObject.SetActive(true);
            yield return new WaitForSeconds(notificationDuration);
            notificationText.text = "";
            notificationBackground.color = Color.clear;
        }
        isDisplaying = false;
    }
}
