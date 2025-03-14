// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using HUD;
// using RWCustom;
// using Menu.Remix.MixedUI;

// namespace GhostPlayer.GHud
// {
//     internal class GAnnouncementHud : GHUDPart
//     {
//         public static GAnnouncementHud Instance { get; private set; }

//         FContainer Container => hud.container;
//         public Vector2 Screen => Custom.rainWorld.screenSize;
//         public Vector2 HoverPos => Screen + new Vector2(-400f, -300f);

//         public List<Announcement> activeAnnouncement = new List<Announcement>();


//         bool lastConnected = true;
//         List<string> slugInRegion = new List<string>();

//         public GAnnouncementHud(GHUD hud) : base(hud)
//         {
//             Instance = this;

//             // if (GhostClient.Instance.IsConnected)
//             // {
//             //     NewAnnouncement("you are connected, welcome!", 10 * 40, AnnouncementType.Good);
//             // }
//             // else
//             //     NewAnnouncement("cannot connect to server.", 10 * 40, AnnouncementType.Warning);
//         }

//         public override void Draw(float timeStacker)
//         {
//             // base.Draw(timeStacker);

//             float highestAlpha = 0f;
//             foreach (var announcement in activeAnnouncement)
//             {
//                 if (announcement.MyAlpha > highestAlpha)
//                     highestAlpha = announcement.MyAlpha;
//             }

//             Vector2 pos = HoverPos;
//             foreach (var announcement in activeAnnouncement)
//             {
//                 float edgeAlpha = Mathf.InverseLerp(0, 80f, Screen.y - pos.y);
//                 announcement.Draw(timeStacker, pos, highestAlpha * edgeAlpha);

//                 pos += Vector2.up * (announcement.TextRect.y + 10f);
//             }
//         }

//         public override void Update()
//         {
//             base.Update();
//             for (int i = activeAnnouncement.Count - 1; i >= 0; i--)
//             {
//                 activeAnnouncement[i].Update();
//             }

//             #region SyncConnect
//             bool thisConnected = GhostClient.Instance != null && GhostClient.Instance.IsConnected;
//             if (lastConnected != thisConnected && !thisConnected)
//             {
//                 NewAnnouncement("you are disconnected!", 10 * 40, AnnouncementType.Warning);
//             }

//             lastConnected = thisConnected;

//             #endregion

//             #region SyncSlug
//             if (HudHooks.stateHud != null && HudHooks.stateHud.roomList != null)
//             {
//                 List<string> newSlugList = new List<string>();
//                 foreach (var room in HudHooks.stateHud.roomList)
//                 {
//                     newSlugList.Add(room.name.text);
//                 }

//                 foreach (var slug in slugInRegion)
//                 {
//                     if (!newSlugList.Contains(slug))
//                         NewAnnouncement($"{slug} left this region", 40, AnnouncementType.Default);
//                 }
//                 foreach (var slug in newSlugList)
//                 {
//                     if (!slugInRegion.Contains(slug))
//                         NewAnnouncement($"{slug} join this region", 40, AnnouncementType.Default);
//                 }
//                 slugInRegion = newSlugList;
//             }
//             else
//                 slugInRegion.Clear();

//             #endregion
//         }

//         public override void ClearSprites()
//         {
//             base.ClearSprites();

//             for (int i = activeAnnouncement.Count - 1; i >= 0; i--)
//             {
//                 activeAnnouncement[i].Destroy();
//             }

//             Instance = null;
//         }

//         public static Color AnnouncementColor(AnnouncementType type)
//         {
//             switch (type)
//             {
//                 case AnnouncementType.Warning:
//                     return GHUDStatic.GHUDyellow;
//                 case AnnouncementType.Error:
//                     return GHUDStatic.GHUDred;
//                 case AnnouncementType.Good:
//                     return GHUDStatic.GHUDgreen;
//                 default:
//                     return GHUDStatic.GHUDwhite;
//             }
//         }

//         public static void NewAnnouncement(string message, int stayTime, AnnouncementType type)
//         {
//             if (Instance == null)
//                 return;
//             Announcement announcement;
//             Instance.activeAnnouncement.Insert(0, announcement = new Announcement(Instance, message, stayTime, AnnouncementColor(type)));
//             announcement.InitiateSprites(Instance.HoverPos + Vector2.right * 300);
//         }

//         public class Announcement
//         {
//             static int fadeTime = 160;
//             static int maxLineLength = 50;

//             #region properties
//             public string Message { get; private set; }
//             public int StayTime { get; private set; }
//             public float MyAlpha
//             {
//                 get
//                 {
//                     return Mathf.Clamp01(1f - (life - StayTime) / (float)fadeTime);
//                 }
//             }
//             public Color MessageColor { get; private set; }
//             public Vector2 TextRect { get; private set; }

//             #endregion

//             GAnnouncementHud announcementHUD;
//             FLabel label;

//             int life;
//             bool lazy;

//             public Announcement(GAnnouncementHud announcementHud, string message, int stayTime, Color color)
//             {
//                 Message = WarpText(message);
//                 StayTime = stayTime;
//                 MessageColor = color;

//                 announcementHUD = announcementHud;
//             }

//             string WarpText(string text)
//             {
//                 StringBuilder stringBuilder = new StringBuilder($"[{DateTime.Now.ToString("T")}]");

//                 int warpedLenght = 0;
//                 for (int i = 0; i < text.Length; i++)
//                 {
//                     warpedLenght++;
//                     char current = text[i];
//                     if ((char.IsPunctuation(current) || current == ' ') && warpedLenght >= maxLineLength)
//                     {
//                         stringBuilder.Append(current);
//                         stringBuilder.Append('\n');
//                         warpedLenght = 0;
//                         continue;
//                     }
//                     if (current == '\n')
//                         warpedLenght = 0;

//                     stringBuilder.Append(current);
//                 }

//                 return stringBuilder.ToString();
//             }

//             public void InitiateSprites(Vector2 startPos)
//             {
//                 label = new FLabel(Custom.GetFont(), Message) { color = MessageColor, scale = 1.1f, anchorX = 0, anchorY = 0, x = startPos.x, y = startPos.y };//左下角定位点
//                 announcementHUD.Container.AddChild(label);

//                 TextRect = new Vector2(label.textRect.width, label.textRect.height);
//             }

//             public void Update()
//             {
//                 life++;
//                 if (life >= fadeTime + StayTime)
//                     lazy = true;
//                 if (life >= StayTime * 10f)
//                     Destroy();
//             }

//             public void Draw(float timeStacker, Vector2 anchorPos, float alpha)
//             {
//                 if (alpha < 0.001)
//                 {
//                     label.isVisible = false;
//                     return;
//                 }

//                 label.isVisible = true;
//                 label.SetPosition(Vector2.Lerp(label.GetPosition(), anchorPos, 0.15f));
//                 label.alpha = alpha;
//             }

//             public void Destroy()
//             {
//                 announcementHUD.Container.RemoveChild(label);
//                 announcementHUD.activeAnnouncement.Remove(this);
//             }
//         }

//         public enum AnnouncementType
//         {
//             Default = 0,
//             Warning,
//             Error,
//             Good
//         }
//     }
// }
