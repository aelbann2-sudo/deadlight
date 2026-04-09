using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Deadlight.Visuals
{
    public class DecalManager : MonoBehaviour
    {
        public static DecalManager Instance { get; private set; }
        
        private const int MaxDecals = 50;
        private const int MaxCorpses = 20;
        private Queue<GameObject> decalPool = new Queue<GameObject>();
        private Queue<GameObject> corpsePool = new Queue<GameObject>();
        
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        
        public void SpawnBloodDecal(Vector3 position, float scale = 1f)
        {
            if (decalPool.Count >= MaxDecals)
            {
                var oldest = decalPool.Dequeue();
                if (oldest != null) Destroy(oldest);
            }
            
            var decal = new GameObject("BloodDecal");
            decal.transform.position = position + (Vector3)(Random.insideUnitCircle * 0.2f);
            decal.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            decal.transform.localScale = Vector3.one * scale * Random.Range(0.5f, 1.2f);
            
            var sr = decal.AddComponent<SpriteRenderer>();
            sr.sprite = CreateBloodSprite();
            sr.sortingOrder = -5;
            sr.color = new Color(0.5f, 0f, 0f, 0.7f);
            
            decalPool.Enqueue(decal);
            StartCoroutine(FadeAndDestroy(decal, sr, 30f));
        }
        
        public void SpawnCorpse(Vector3 position, Sprite zombieSprite, Color tint)
        {
            if (corpsePool.Count >= MaxCorpses)
            {
                var oldest = corpsePool.Dequeue();
                if (oldest != null) Destroy(oldest);
            }
            
            var corpse = new GameObject("Corpse");
            corpse.transform.position = position;
            corpse.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
            
            var sr = corpse.AddComponent<SpriteRenderer>();
            sr.sprite = zombieSprite;
            sr.sortingOrder = -4;
            sr.color = new Color(tint.r * 0.5f, tint.g * 0.5f, tint.b * 0.5f, 0.8f);
            
            corpsePool.Enqueue(corpse);
            StartCoroutine(FadeAndDestroy(corpse, sr, 10f));
        }
        
        IEnumerator FadeAndDestroy(GameObject obj, SpriteRenderer sr, float lifetime)
        {
            float fadeStart = lifetime * 0.7f;
            float elapsed = 0f;
            Color startColor = sr.color;
            
            while (elapsed < lifetime && obj != null)
            {
                elapsed += Time.deltaTime;
                if (elapsed > fadeStart && sr != null)
                {
                    float t = (elapsed - fadeStart) / (lifetime - fadeStart);
                    sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
                }
                yield return null;
            }
            
            if (obj != null) Destroy(obj);
        }
        
        Sprite CreateBloodSprite()
        {
            int size = 16;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    float noise = Random.Range(-0.2f, 0.2f);
                    pixels[y * size + x] = (dist + noise) < 0.8f
                        ? new Color(0.6f, 0f, 0f, Mathf.Clamp01(1f - dist))
                        : Color.clear;
                }
            
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
        
        public void ClearAll()
        {
            while (decalPool.Count > 0) { var d = decalPool.Dequeue(); if (d != null) Destroy(d); }
            while (corpsePool.Count > 0) { var c = corpsePool.Dequeue(); if (c != null) Destroy(c); }
        }
    }
}
