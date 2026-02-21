using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Systems
{
    public class HelicopterDrop : MonoBehaviour
    {
        private Vector3 targetPosition;
        private Vector3 exitPosition;
        private CrateTier crateTier = CrateTier.Common;
        private float flySpeed = 8f;
        private bool hasDropped;
        private bool exiting;
        private SpriteRenderer sr;
        private AudioSource audioSource;
        private AudioClip rotorClip;

        public void Initialize(Vector3 dropTarget, CrateTier tier)
        {
            targetPosition = dropTarget;
            crateTier = tier;

            Camera cam = Camera.main;
            float halfW = cam != null ? cam.orthographicSize * cam.aspect + 5f : 20f;
            float y = dropTarget.y + 6f;

            bool fromLeft = Random.value > 0.5f;
            float startX = fromLeft ? dropTarget.x - halfW * 2f : dropTarget.x + halfW * 2f;
            float exitX = fromLeft ? dropTarget.x + halfW * 2f : dropTarget.x - halfW * 2f;

            transform.position = new Vector3(startX, y, 0);
            exitPosition = new Vector3(exitX, y, 0);

            if (!fromLeft)
                transform.localScale = new Vector3(-1, 1, 1);

            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateHelicopterSprite();
            sr.sortingOrder = 200;
            sr.color = new Color(0.3f, 0.35f, 0.3f);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.5f;
            audioSource.volume = 0.3f;
            audioSource.loop = true;
            try
            {
                rotorClip = Audio.ProceduralAudioGenerator.GenerateAmbientWind();
                if (rotorClip != null)
                {
                    audioSource.clip = rotorClip;
                    audioSource.Play();
                }
            }
            catch { }

            var rotorGo = new GameObject("Rotor");
            rotorGo.transform.SetParent(transform);
            rotorGo.transform.localPosition = new Vector3(0, 0.3f, 0);
            var rotorSr = rotorGo.AddComponent<SpriteRenderer>();
            rotorSr.sprite = CreateRotorSprite();
            rotorSr.sortingOrder = 201;
            rotorSr.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            rotorGo.AddComponent<RotorSpin>();
        }

        private void Update()
        {
            Vector3 moveTarget = hasDropped ? exitPosition : new Vector3(targetPosition.x, transform.position.y, 0);
            Vector3 dir = (moveTarget - transform.position).normalized;
            transform.position += dir * flySpeed * Time.deltaTime;

            if (!hasDropped && Mathf.Abs(transform.position.x - targetPosition.x) < 0.5f)
            {
                DropCrate();
            }

            if (hasDropped && Vector3.Distance(transform.position, exitPosition) < 2f)
            {
                Destroy(gameObject);
            }
        }

        private void DropCrate()
        {
            hasDropped = true;

            var crateObj = new GameObject("HeliCrate");
            crateObj.transform.position = new Vector3(targetPosition.x, transform.position.y, 0);
            var fallingCrate = crateObj.AddComponent<FallingCrate>();
            fallingCrate.Initialize(targetPosition, crateTier);
        }

        private static Sprite CreateHelicopterSprite()
        {
            int w = 48, h = 24;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            for (int x = 8; x < 40; x++)
                for (int y = 6; y < 16; y++)
                    px[y * w + x] = Color.white;

            for (int x = 0; x < 12; x++)
                for (int y = 8; y < 13; y++)
                    px[y * w + x] = new Color(0.8f, 0.8f, 0.8f);

            for (int x = 36; x < 48; x++)
                for (int y = 10; y < 14; y++)
                    px[y * w + x] = new Color(0.7f, 0.7f, 0.7f);

            for (int x = 18; x < 30; x++)
                for (int y = 3; y < 7; y++)
                    px[y * w + x] = new Color(0.9f, 0.9f, 0.9f);

            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
        }

        private static Sprite CreateRotorSprite()
        {
            int w = 64, h = 4;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            for (int x = 0; x < w; x++)
                for (int y = 1; y < 3; y++)
                    px[y * w + x] = new Color(0.6f, 0.6f, 0.6f, 0.7f);

            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
        }
    }

    public class RotorSpin : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(0, 0, 1200f * Time.deltaTime);
        }
    }

    public class FallingCrate : MonoBehaviour
    {
        private Vector3 landTarget;
        private CrateTier tier;
        private float fallSpeed = 5f;
        private SpriteRenderer crateSr;
        private SpriteRenderer parachuteSr;

        public void Initialize(Vector3 target, CrateTier t)
        {
            landTarget = target;
            tier = t;

            crateSr = gameObject.AddComponent<SpriteRenderer>();
            crateSr.sprite = CreateFallingCrateSprite();
            crateSr.sortingOrder = 150;
            crateSr.color = tier switch
            {
                CrateTier.Legendary => new Color(1f, 0.85f, 0.2f),
                CrateTier.Rare => new Color(0.3f, 0.5f, 1f),
                _ => new Color(0.6f, 0.45f, 0.2f)
            };

            var paraObj = new GameObject("Parachute");
            paraObj.transform.SetParent(transform);
            paraObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            parachuteSr = paraObj.AddComponent<SpriteRenderer>();
            parachuteSr.sprite = CreateParachuteSprite();
            parachuteSr.sortingOrder = 149;
            parachuteSr.color = Color.white;
        }

        private void Update()
        {
            if (transform.position.y > landTarget.y)
            {
                transform.position += Vector3.down * fallSpeed * Time.deltaTime;
                float sway = Mathf.Sin(Time.time * 3f) * 0.3f;
                transform.position += Vector3.right * sway * Time.deltaTime;
            }
            else
            {
                Land();
            }
        }

        private void Land()
        {
            if (parachuteSr != null) Destroy(parachuteSr.gameObject);

            var sc = gameObject.AddComponent<SupplyCrate>();
            sc.SetTier(tier);

            if (Core.GameEffects.Instance != null)
                Core.GameEffects.Instance.ScreenShake(0.1f, 0.15f);

            Destroy(this);
        }

        private static Sprite CreateFallingCrateSprite()
        {
            int s = 20;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    bool border = x < 2 || x >= s - 2 || y < 2 || y >= s - 2;
                    bool cross = Mathf.Abs(x - s / 2) < 2 || Mathf.Abs(y - s / 2) < 2;
                    px[y * s + x] = border ? new Color(0.35f, 0.25f, 0.1f) :
                                    cross ? new Color(0.45f, 0.35f, 0.15f) :
                                    Color.white;
                }
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static Sprite CreateParachuteSprite()
        {
            int w = 32, h = 24;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            Vector2 center = new Vector2(w / 2f, h * 0.8f);
            for (int y = h / 2; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - center.x) / (w * 0.45f);
                    float dy = (y - center.y) / (h * 0.5f);
                    if (dx * dx + dy * dy < 1f)
                        px[y * w + x] = new Color(0.95f, 0.95f, 0.95f, 0.9f);
                }

            int midX = w / 2;
            for (int y = 0; y < h / 2; y++)
            {
                int lx = Mathf.RoundToInt(Mathf.Lerp(midX, 4, (float)y / (h / 2)));
                int rx = Mathf.RoundToInt(Mathf.Lerp(midX, w - 4, (float)y / (h / 2)));
                if (lx >= 0 && lx < w) px[y * w + lx] = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                if (rx >= 0 && rx < w) px[y * w + rx] = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }

            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
        }
    }
}
