using UnityEngine;

namespace Deadlight.Level
{
    public class FireEffect : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float flickerTimer;

        private void Start()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFireSprite();
            sr.sortingOrder = 20;
        }

        private void Update()
        {
            flickerTimer += Time.deltaTime * 8f;
            float scale = 1f + Mathf.Sin(flickerTimer) * 0.2f;
            transform.localScale = new Vector3(scale, scale * 1.2f, 1f);
            sr.color = new Color(1f, 0.6f + Mathf.Sin(flickerTimer * 1.3f) * 0.2f, 0.2f, 0.8f);
        }

        private Sprite CreateFireSprite()
        {
            const int size = 16;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 4f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = y - center.y;
                    if (dy > 0f && dx < (size / 2f) * (1f - dy / size))
                    {
                        pixels[y * size + x] = Color.Lerp(
                            new Color(1f, 0.6f, 0.1f),
                            new Color(1f, 0.2f, 0f, 0f),
                            dy / size);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), 16f);
        }
    }

    public class SmokeEffect : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float driftTimer;

        private void Start()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSmokeSprite();
            sr.sortingOrder = 19;
            sr.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
        }

        private void Update()
        {
            driftTimer += Time.deltaTime;
            float drift = Mathf.Sin(driftTimer * 0.5f) * 0.3f;
            transform.localPosition = new Vector3(0.5f + drift, 0.8f + driftTimer % 2f * 0.5f, 0f);
            float alpha = 0.4f * (1f - (driftTimer % 2f) / 2f);
            sr.color = new Color(0.3f, 0.3f, 0.3f, alpha);
            transform.localScale = Vector3.one * (1f + (driftTimer % 2f) * 0.5f);
        }

        private Sprite CreateSmokeSprite()
        {
            const int size = 24;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    if (dist < 1f)
                    {
                        pixels[y * size + x] = new Color(0.5f, 0.5f, 0.5f, (1f - dist) * 0.6f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }

    public class SearchlightEffect : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float rotationTimer;

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
            }

            if (sr == null)
            {
                enabled = false;
                return;
            }

            Sprite beamSprite = CreateBeamSprite();
            if (beamSprite == null)
            {
                enabled = false;
                return;
            }

            sr.sprite = beamSprite;
            sr.sortingOrder = -50;
            transform.localScale = new Vector3(1f, 4f, 1f);
        }

        private void Update()
        {
            rotationTimer += Time.deltaTime * 20f;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationTimer);
        }

        private Sprite CreateBeamSprite()
        {
            const int w = 16;
            const int h = 64;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w / 2f) / (w / 2f);
                    float dy = (float)y / h;
                    pixels[y * w + x] = new Color(1f, 1f, 0.8f, (1f - dx) * (1f - dy) * 0.3f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        }
    }

    public class FlickeringLight : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float flickerTimer;
        private float nextFlicker;
        private bool isOn = true;

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = GetComponentInChildren<SpriteRenderer>();
            }

            nextFlicker = Random.Range(0.5f, 3f);
        }

        private void Update()
        {
            flickerTimer += Time.deltaTime;
            if (flickerTimer < nextFlicker)
            {
                return;
            }

            flickerTimer = 0f;
            nextFlicker = Random.Range(0.1f, 2f);
            isOn = !isOn || Random.value > 0.3f;
            if (sr == null)
            {
                return;
            }

            Color color = sr.color;
            color.a = isOn ? 0.25f : 0.05f;
            sr.color = color;
        }
    }

    public class GlowPulse : MonoBehaviour
    {
        private void Update()
        {
            float pulse = 0.8f + Mathf.Sin(Time.time * 2f) * 0.2f;
            transform.localScale = Vector3.one * pulse * 2f;
        }
    }
}
