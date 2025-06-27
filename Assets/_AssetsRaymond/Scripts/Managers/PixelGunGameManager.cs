using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class PixelGunGameManager : MonoBehaviourPunCallbacks
{
    [SerializeField]

    GameObject playerPrefab;

    GameObject Controller;
    PhotonView PV;

    public static PixelGunGameManager instance;

    //public static PixelGunGameManager Instance;

    private void Awake()
    {
        if(instance!=null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }

        PV = playerPrefab.GetComponent<PhotonView>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            CreateController();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(PhotonNetwork.NickName + " joined to " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer.NickName + " joined to " + PhotonNetwork.CurrentRoom.Name + " " + PhotonNetwork.CurrentRoom.PlayerCount);
    }

    void CreateController()
    {
        if (playerPrefab != null)
        {
            int randomPoint = Random.Range(-20, 20);

            Controller = PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(randomPoint, 0, randomPoint), Quaternion.identity, 0, new object[] { PV.ViewID });
        }
    }

    public void Die()
    {
        PhotonNetwork.Destroy(Controller);
        CreateController();
    }
}
