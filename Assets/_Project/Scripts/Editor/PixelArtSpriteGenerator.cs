#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WildFrontier.Editor
{
    /// <summary>
    /// [가디언 테일즈(ref/img) 스타일 현대식 하이브리드 픽셀아트 생성기]
    /// 1. 배경/프롭: 저해상도 모자이크가 아닌 고해상도 소프트 페인팅(Bilinear) 잔디 그리드, 거대 그루터기, 둥근 숲 나무, 황토길, 대각선 햇살(Sunbeam)
    /// 2. 캐릭터/몬스터: 따뜻한 컬러 외곽선의 2등신 SD 도트 + 파츠 분리형 오버사이즈 무기 & 반투명 슬래시 검기
    /// 3. UI: 모던 원형 버튼 및 날개 부츠 아이콘
    /// </summary>
    public static class PixelArtSpriteGenerator
    {
        private static readonly Color C_Trans = Color.clear;
        private const string GeneratedSheetAbsPath = "Assets/_Project/Art/SourceSheets/sd_chibi_classes_sheet.jpg";
        private const string MonstersSheetAbsPath = "Assets/_Project/Art/SourceSheets/modern_pixel_monsters_sheet.jpg";
        private const string PropsSheetAbsPath = "Assets/_Project/Art/SourceSheets/modern_pixel_props_sheet.jpg";

        public static Sprite TryExtractSDChibiClassSprite(string folder, string outName, float xMinNorm, float xMaxNorm, float yMinNorm, float yMaxNorm)
        {
            if (!File.Exists(GeneratedSheetAbsPath))
                return null;

            byte[] rawBytes = File.ReadAllBytes(GeneratedSheetAbsPath);
            Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!srcTex.LoadImage(rawBytes))
            {
                Object.DestroyImmediate(srcTex);
                return null;
            }

            int x0 = Mathf.Clamp(Mathf.RoundToInt(srcTex.width * xMinNorm), 0, srcTex.width - 1);
            int x1 = Mathf.Clamp(Mathf.RoundToInt(srcTex.width * xMaxNorm), x0 + 1, srcTex.width);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(srcTex.height * yMinNorm), 0, srcTex.height - 1);
            int y1 = Mathf.Clamp(Mathf.RoundToInt(srcTex.height * yMaxNorm), y0 + 1, srcTex.height);

            int w = x1 - x0;
            int h = y1 - y0;
            Color[] pixels = srcTex.GetPixels(x0, y0, w, h);
            Object.DestroyImmediate(srcTex);

            bool[] visited = new bool[w * h];
            Queue<int> queue = new Queue<int>();

            for (int bx = 0; bx < w; bx++)
            {
                EnqueueIfBg(bx, 0, w, h, pixels, visited, queue);
                EnqueueIfBg(bx, h - 1, w, h, pixels, visited, queue);
            }
            for (int by = 0; by < h; by++)
            {
                EnqueueIfBg(0, by, w, h, pixels, visited, queue);
                EnqueueIfBg(w - 1, by, w, h, pixels, visited, queue);
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                pixels[idx] = Color.clear;
                int cx = idx % w;
                int cy = idx / w;

                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d];
                    int ny = cy + dy[d];
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                        EnqueueIfBg(nx, ny, w, h, pixels, visited, queue);
                }
            }

            // 2단계: 다리 사이 등 내부에 갇힌 크림색 모눈종이 픽셀 제거
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    Color c = pixels[idx];
                    if (c.a > 0f && c.r > 0.91f && c.g > 0.88f && c.b > 0.77f && (c.r - c.g) < 0.06f && (c.g - c.b) < 0.14f)
                    {
                        // 눈동자 하이라이트(상단 얼굴 영역)는 보존하고 하단/외곽 여백만 투명화
                        if (y < h * 0.42f || x < w * 0.22f || x > w * 0.78f)
                            pixels[idx] = Color.clear;
                    }
                }
            }

            // 3단계: 인접 캐릭터 무기 파편 제거 (가장 큰 연결 컴포넌트인 메인 캐릭터만 남김)
            KeepLargestConnectedComponent(pixels, w, h);

            Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels(pixels);
            outTex.Apply();

            string path = $"{folder}/{outName}.png";
            File.WriteAllBytes(path, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single; // ★ 핵심: Multiple(32x32 구석 슬라이스) 방지!
                importer.spritePixelsPerUnit = h * 0.55f;
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                TextureImporterSettings tis = new TextureImporterSettings();
                importer.ReadTextureSettings(tis);
                tis.spriteMode = (int)SpriteImportMode.Single;
                importer.SetTextureSettings(tis);

                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void KeepLargestConnectedComponent(Color[] pixels, int w, int h)
        {
            int[] compId = new int[w * h];
            int currentComp = 0;
            int bestComp = -1;
            int bestCount = 0;
            Queue<int> q = new Queue<int>();
            int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 0.1f || compId[i] != 0) continue;
                currentComp++;
                int count = 0;
                compId[i] = currentComp;
                q.Enqueue(i);

                while (q.Count > 0)
                {
                    int cur = q.Dequeue();
                    count++;
                    int cx = cur % w;
                    int cy = cur / w;

                    for (int d = 0; d < 8; d++)
                    {
                        int nx = cx + dx[d];
                        int ny = cy + dy[d];
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                        {
                            int nIdx = ny * w + nx;
                            if (compId[nIdx] == 0 && pixels[nIdx].a >= 0.1f)
                            {
                                compId[nIdx] = currentComp;
                                q.Enqueue(nIdx);
                            }
                        }
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestComp = currentComp;
                }
            }

            if (bestComp > 0)
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (compId[i] != 0 && compId[i] != bestComp)
                        pixels[i] = Color.clear;
                }
            }
        }

        private static void EnqueueIfBg(int x, int y, int w, int h, Color[] pixels, bool[] visited, Queue<int> queue)
        {
            int idx = y * w + x;
            if (visited[idx]) return;

            Color c = pixels[idx];
            bool isCreamGrid = (c.r > 0.78f && c.g > 0.74f && c.b > 0.62f && (c.r - c.b) < 0.25f);
            bool isFloorShadow = (y < h * 0.16f && c.r > 0.50f && c.r < 0.72f && c.g > 0.38f && c.g < 0.58f && c.b > 0.24f && c.b < 0.44f);

            if (isCreamGrid || isFloorShadow)
            {
                visited[idx] = true;
                queue.Enqueue(idx);
            }
        }

        /// <summary>
        /// 순백색(#FFFFFF) 배경의 고밀도 픽셀 스프라이트 시트에서 개별 몬스터·프롭·아이콘 영역을 크롭하여
        /// 외곽 BFS Flood-Fill + 닫힌 순백색 틈새 제거 + 타이트 트리밍 후 Single 스프라이트로 임포트합니다.
        /// </summary>
        public static Sprite TryExtractFromWhiteSheet(
            string sheetAbsPath,
            string folder,
            string outName,
            float xMinNorm,
            float xMaxNorm,
            float yMinNorm,
            float yMaxNorm,
            float targetWorldHeight,
            bool keepSparkles = false)
        {
            if (!File.Exists(sheetAbsPath))
                return null;

            byte[] rawBytes = File.ReadAllBytes(sheetAbsPath);
            Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!srcTex.LoadImage(rawBytes))
            {
                Object.DestroyImmediate(srcTex);
                return null;
            }

            int x0 = Mathf.Clamp(Mathf.RoundToInt(srcTex.width * xMinNorm), 0, srcTex.width - 1);
            int x1 = Mathf.Clamp(Mathf.RoundToInt(srcTex.width * xMaxNorm), x0 + 1, srcTex.width);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(srcTex.height * yMinNorm), 0, srcTex.height - 1);
            int y1 = Mathf.Clamp(Mathf.RoundToInt(srcTex.height * yMaxNorm), y0 + 1, srcTex.height);

            int w = x1 - x0;
            int h = y1 - y0;
            Color[] pixels = srcTex.GetPixels(x0, y0, w, h);
            Object.DestroyImmediate(srcTex);

            // 1단계: 외곽 테두리 BFS Flood-Fill로 흰색/연회색 배경 투명화
            bool[] visited = new bool[w * h];
            Queue<int> queue = new Queue<int>();

            for (int bx = 0; bx < w; bx++)
            {
                EnqueueIfWhiteBg(bx, 0, w, pixels, visited, queue);
                EnqueueIfWhiteBg(bx, h - 1, w, pixels, visited, queue);
            }
            for (int by = 0; by < h; by++)
            {
                EnqueueIfWhiteBg(0, by, w, pixels, visited, queue);
                EnqueueIfWhiteBg(w - 1, by, w, pixels, visited, queue);
            }

            int[] dx4 = { 1, -1, 0, 0 };
            int[] dy4 = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                pixels[idx] = Color.clear;
                int cx = idx % w;
                int cy = idx / w;

                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx4[d];
                    int ny = cy + dy4[d];
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                        EnqueueIfWhiteBg(nx, ny, w, pixels, visited, queue);
                }
            }

            // 2단계: 모닥불 삼각대·벌레 다리·뿌리 사이 등 닫힌 내부의 넓은 순백색 배경 틈새 제거
            // (반경 4px 이하의 작은 눈동자 하이라이트는 보존)
            RemoveEnclosedWhitePockets(pixels, w, h, minPocketPixelCount: 25);

            // 3단계: 인접 파편 제거 (반짝임 유지 옵션에 따라 분기)
            if (keepSparkles)
                RemoveTinyComponents(pixels, w, h, minComponentPixels: 4);
            else
                KeepLargestConnectedComponent(pixels, w, h);

            // 4단계: 상하좌우 4px 여백만 남기고 타이트하게 트리밍
            Color[] trimmedPixels = TrimTransparentPadding(pixels, w, h, 4, out int trimmedW, out int trimmedH);

            Texture2D outTex = new Texture2D(trimmedW, trimmedH, TextureFormat.RGBA32, false);
            outTex.SetPixels(trimmedPixels);
            outTex.Apply();

            string path = $"{folder}/{outName}.png";
            File.WriteAllBytes(path, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = trimmedH / Mathf.Max(0.2f, targetWorldHeight);
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                TextureImporterSettings tis = new TextureImporterSettings();
                importer.ReadTextureSettings(tis);
                tis.spriteMode = (int)SpriteImportMode.Single;
                importer.SetTextureSettings(tis);

                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnqueueIfWhiteBg(int x, int y, int w, Color[] pixels, bool[] visited, Queue<int> queue)
        {
            int idx = y * w + x;
            if (visited[idx]) return;

            Color c = pixels[idx];
            if (c.r > 0.88f && c.g > 0.88f && c.b > 0.86f && Mathf.Abs(c.r - c.b) < 0.08f)
            {
                visited[idx] = true;
                queue.Enqueue(idx);
            }
        }

        private static void RemoveEnclosedWhitePockets(Color[] pixels, int w, int h, int minPocketPixelCount)
        {
            bool[] checkedWhite = new bool[w * h];
            Queue<int> q = new Queue<int>();
            List<int> comp = new List<int>();
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            for (int i = 0; i < pixels.Length; i++)
            {
                if (checkedWhite[i]) continue;
                Color c = pixels[i];
                if (c.a <= 0.1f || c.r <= 0.94f || c.g <= 0.94f || c.b <= 0.94f || Mathf.Abs(c.r - c.b) >= 0.04f)
                    continue;

                comp.Clear();
                checkedWhite[i] = true;
                q.Enqueue(i);
                int outlineNeighborCount = 0;
                int interiorNeighborCount = 0;

                while (q.Count > 0)
                {
                    int cur = q.Dequeue();
                    comp.Add(cur);
                    int cx = cur % w;
                    int cy = cur / w;

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + dx[d];
                        int ny = cy + dy[d];
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                        int nIdx = ny * w + nx;
                        Color nc = pixels[nIdx];
                        if (nc.a > 0.1f && nc.r > 0.94f && nc.g > 0.94f && nc.b > 0.94f && Mathf.Abs(nc.r - nc.b) < 0.04f)
                        {
                            if (!checkedWhite[nIdx])
                            {
                                checkedWhite[nIdx] = true;
                                q.Enqueue(nIdx);
                            }
                        }
                        else if (nc.a > 0.1f)
                        {
                            // 어두운 외곽선 픽셀(모닥불 다리, 벌레 다리 등)과 밝은 내부 색상을 구분
                            if (nc.r < 0.42f && nc.g < 0.38f && nc.b < 0.38f)
                                outlineNeighborCount++;
                            else
                                interiorNeighborCount++;
                        }
                    }
                }

                // 순백색 영역이 충분히 크고, 어두운 외곽선으로 둘러싸인 배경 틈새인 경우에만 투명화
                if (comp.Count >= minPocketPixelCount && outlineNeighborCount >= interiorNeighborCount)
                {
                    for (int k = 0; k < comp.Count; k++)
                        pixels[comp[k]] = Color.clear;
                }
            }
        }

        private static void RemoveTinyComponents(Color[] pixels, int w, int h, int minComponentPixels)
        {
            bool[] visited = new bool[w * h];
            Queue<int> q = new Queue<int>();
            List<int> comp = new List<int>();
            int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

            for (int i = 0; i < pixels.Length; i++)
            {
                if (visited[i] || pixels[i].a < 0.1f) continue;
                comp.Clear();
                visited[i] = true;
                q.Enqueue(i);

                while (q.Count > 0)
                {
                    int cur = q.Dequeue();
                    comp.Add(cur);
                    int cx = cur % w;
                    int cy = cur / w;
                    for (int d = 0; d < 8; d++)
                    {
                        int nx = cx + dx[d];
                        int ny = cy + dy[d];
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                        {
                            int nIdx = ny * w + nx;
                            if (!visited[nIdx] && pixels[nIdx].a >= 0.1f)
                            {
                                visited[nIdx] = true;
                                q.Enqueue(nIdx);
                            }
                        }
                    }
                }

                if (comp.Count < minComponentPixels)
                {
                    for (int k = 0; k < comp.Count; k++)
                        pixels[comp[k]] = Color.clear;
                }
            }
        }

        private static Color[] TrimTransparentPadding(Color[] pixels, int w, int h, int padding, out int outW, out int outH)
        {
            int minX = w, maxX = -1, minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a > 0.05f)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX < minX || maxY < minY)
            {
                outW = w;
                outH = h;
                return pixels;
            }

            minX = Mathf.Max(0, minX - padding);
            maxX = Mathf.Min(w - 1, maxX + padding);
            minY = Mathf.Max(0, minY - padding);
            maxY = Mathf.Min(h - 1, maxY + padding);

            outW = maxX - minX + 1;
            outH = maxY - minY + 1;
            Color[] result = new Color[outW * outH];

            for (int y = 0; y < outH; y++)
            {
                for (int x = 0; x < outW; x++)
                {
                    result[y * outW + x] = pixels[(minY + y) * w + (minX + x)];
                }
            }
            return result;
        }

        public static Sprite GenerateHunterSprite(string folder)
        {
            Sprite extracted = TryExtractSDChibiClassSprite(folder, "Spr_Hunter_32", 0.04f, 0.385f, 0.06f, 0.84f);
            if (extracted != null) return extracted;

            string[] map =
            {
                "................................",
                ".............OOOO...............",
                "............OTTTTO..............",
                "..........OOOTWTTTOOO...........",
                "........OOYYYTTTTTYYYOO.........",
                ".......OYYDDFTTTTTFFDDYYO.......",
                "......OYYDDFYYYYYYYYFDDYYO......",
                ".....OYYDDYYYYHYYYYYYDDYYYO.....",
                ".....OYYDYYYYHHHYYYYYYDYYYO.....",
                "....OYYYYYYYYHYYYYYYYYYYYYYO....",
                "...OOYYYYYYYYYYYYYYYYYYYYYYOO...",
                "..OYYYYYYYYYYYYYYYYYYYYYYYYYYO..",
                "...OYYYYYOOOOYYYYYYOOOOYYYYYO...",
                "....OYYYOSSSSOYYYYOSSSSOYYYO....",
                "....OYYOSSSSSSOYYOSSSSSSOYYO....",
                "....OYOSSNNSNSSOSSNSNNSSOYO.....",
                "....OOYOSSEESSSSSSSEESSOYYO.....",
                ".....OYOSSEBSSSSSSSBESSOYO......",
                ".....OYYOPPPSSSMMSSSSPPYYO......",
                "......OOYSSSSSSMMSSSSSYYOO......",
                ".......OOYOOOOOOOOOOOOYOO.......",
                ".........ORRRBBYYBBRRRO.........",
                "........ORRRRBBYYBBRRRRO........",
                ".......OSSRROOOOOOOORRSSO.......",
                ".......OSSO..OAAAAO..OSSO.......",
                "........OO...OAAAAO...OO........",
                ".............OKKKKO.............",
                ".............OKOOKO.............",
                ".............OO..OO.............",
                "................................",
                "................................",
                "................................"
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(125, 62, 28, 255),
                ['T'] = new Color32(16, 150, 156, 255),
                ['W'] = new Color32(210, 255, 250, 255),
                ['D'] = new Color32(92, 82, 102, 255),
                ['F'] = new Color32(142, 132, 155, 255),
                ['Y'] = new Color32(250, 214, 67, 255),
                ['H'] = new Color32(255, 242, 140, 255),
                ['S'] = new Color32(252, 222, 194, 255),
                ['N'] = new Color32(72, 34, 20, 255),
                ['E'] = new Color32(16, 126, 158, 255),
                ['B'] = new Color32(76, 240, 255, 255),
                ['P'] = new Color32(244, 134, 154, 255),
                ['M'] = new Color32(168, 62, 40, 255),
                ['R'] = new Color32(204, 44, 58, 255),
                ['A'] = new Color32(228, 138, 38, 255),
                ['K'] = new Color32(62, 30, 20, 255),
            };

            return SaveGridSprite(folder, "Spr_Hunter_32", map, pal, 2, ppu: 44f);
        }

        /// <summary>
        /// [파츠 분리형 오버사이즈 대검] 캐릭터 손 피벗에 장착되어 공격 시 호쾌하게 회전 스윙하는 무기 파츠입니다.
        /// </summary>
        public static Sprite GenerateOversizedWeaponSprite(string folder)
        {
            string[] map =
            {
                ".......OO.......",
                "......OWWO......",
                ".....OWHBWO.....",
                ".....OWHBWO.....",
                "....OOWHBWOO....",
                "....OWWHBWWO....",
                "....OWWHBWWO....",
                "...OOWWHBWWOO...",
                "...OWWWHBWWWO...",
                "...OWWWHBWWWO...",
                "...OWWWHBWWWO...",
                "..OOGGGGGGGGOO..",
                "..OGGGGGGGGGGO..",
                "....OOOBBBOO....",
                "......OBBO......",
                "......OOOO......"
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(75, 38, 24, 255),
                ['W'] = new Color32(235, 245, 255, 255), // 검날 하이라이트
                ['H'] = new Color32(255, 165, 45, 255),  // 검신 화염 코어
                ['B'] = new Color32(175, 190, 210, 255), // 검날 그림자
                ['G'] = new Color32(245, 195, 55, 255),  // 황금 코등이
                ['K'] = new Color32(125, 65, 35, 255),
            };

            return SaveGridSprite(folder, "Spr_OversizedSword", map, pal, 3, ppu: 38f);
        }

        /// <summary>
        /// [고해상도 반투명 슬래시 검기] 공격 스윙 시 전방에 터지는 가디언 테일즈풍 부드러운 초승달 검기 이펙트
        /// </summary>
        public static Sprite GenerateSlashArcSprite(string folder)
        {
            string path = $"{folder}/Spr_SlashArc_HD.png";
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size * 0.35f) / (size * 0.5f);
                    float dy = (y - size * 0.5f) / (size * 0.5f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                    if (x > size * 0.35f && r >= 0.45f && r <= 0.95f && Mathf.Abs(angle) <= 72f)
                    {
                        float radialFade = 1f - Mathf.Abs((r - 0.72f) / 0.25f);
                        float angleFade = 1f - Mathf.Pow(Mathf.Abs(angle) / 72f, 1.6f);
                        float alpha = Mathf.Clamp01(radialFade * angleFade * 1.2f);
                        Color col = Color.Lerp(new Color(1f, 0.75f, 0.2f, alpha), new Color(1f, 0.98f, 0.85f, alpha), radialFade);
                        tex.SetPixel(x, y, col);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 64f;
                imp.filterMode = FilterMode.Bilinear; // 현대식 부드러운 이펙트
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Sprite GenerateHornedRabbitSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(MonstersSheetAbsPath, folder, "Spr_Rabbit_32",
                0.03f, 0.31f, 0.52f, 0.97f, targetWorldHeight: 1.05f);
            if (extracted != null) return extracted;

            string[] map =
            {
                "....OOO..OOO....",
                "...OEEO..OEEO...",
                "...OEEO..OEEO...",
                "...OBBO..OBBO...",
                "...OBBBOOBBBO...",
                "..OBBWHHHHWBBO..",
                ".OBBBBBHHBBBBBO.",
                ".OBBNNBBBBNNBBO.",
                "OBBBRWBBBBRWBBBO",
                "OBBPPBBBBBBPPBBO",
                "OBBBBBCCCCBBBBBO",
                ".OBBBBCCCCBBBBO.",
                "..OOBBCCCCBBOO..",
                "...OWWOOOOOWWO..",
                "...OOOO...OOOO..",
                "................"
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(105, 58, 32, 255),
                ['B'] = new Color32(235, 185, 125, 255),
                ['E'] = new Color32(248, 165, 175, 255),
                ['C'] = new Color32(255, 248, 235, 255),
                ['W'] = new Color32(255, 255, 255, 255),
                ['N'] = new Color32(65, 30, 25, 255),
                ['R'] = new Color32(225, 55, 75, 255),
                ['P'] = new Color32(248, 138, 155, 255),
                ['H'] = new Color32(255, 235, 110, 255),
            };

            return SaveGridSprite(folder, "Spr_Rabbit_32", map, pal, 2, ppu: 32f);
        }

        public static Sprite GenerateSporeMushroomMonsterSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(MonstersSheetAbsPath, folder, "Spr_MushroomMon_32",
                0.34f, 0.64f, 0.52f, 0.97f, targetWorldHeight: 1.10f);
            if (extracted != null) return extracted;

            return GenerateGlowMushroomSprite(folder);
        }

        public static Sprite GenerateBerserkDeokguSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(MonstersSheetAbsPath, folder, "Spr_Deokgu_48",
                0.34f, 0.64f, 0.03f, 0.50f, targetWorldHeight: 1.70f, keepSparkles: true);
            if (extracted != null) return extracted;

            string[] map =
            {
                "...O..OO..OO..O.",
                "..OGGOGGOOGGOGGO",
                ".OGGGGGGGGGGGGGO",
                "OGGGGGGGGGGGGGGO",
                ".OODDDDDDDDDDOO.",
                ".ODDDDDDDDDDDDO.",
                "ODDDNNDDDDNNDDDO",
                "ODDDRWDDDDRWDDDO",
                "ODTDDDPPPPDDDTDO",
                "OTTDDDDDDDDDDTTO",
                ".OODDDDDDDDDDOO.",
                "..ODDDCCCCDDDO..",
                "..ODDDCCCCDDDO..",
                "...OKKOOOOKKO...",
                "...OOOO..OOOO...",
                "................"
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(75, 38, 24, 255),
                ['G'] = new Color32(255, 220, 45, 255),
                ['D'] = new Color32(145, 92, 68, 255),
                ['N'] = new Color32(45, 20, 20, 255),
                ['R'] = new Color32(255, 50, 50, 255),
                ['W'] = new Color32(255, 245, 200, 255),
                ['P'] = new Color32(235, 140, 145, 255),
                ['T'] = new Color32(255, 250, 235, 255),
                ['C'] = new Color32(185, 130, 95, 255),
                ['K'] = new Color32(60, 35, 25, 255),
            };

            return SaveGridSprite(folder, "Spr_Deokgu_48", map, pal, 3, ppu: 32f);
        }

        public static Sprite GenerateSpikedBeetleSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(MonstersSheetAbsPath, folder, "Spr_Beetle_48",
                0.67f, 0.98f, 0.55f, 0.95f, targetWorldHeight: 1.30f);
            if (extracted != null) return extracted;

            string[] map =
            {
                ".......OO.......",
                "......OYYO......",
                ".....OYCCYO.....",
                ".....OCCCCO.....",
                "...OOOHCCCHOO...",
                "..OCHCCCCCCCHO..",
                ".OCCCCCCCCCCCCO.",
                ".OOCRRCCCCRRCBO.",
                "OLOCCBCCCCBCCOLO",
                ".OOCBBBBBBBBCOO.",
                "OLOCBBBBBBBBCOLO",
                ".OOCCCCCCCCCCOO.",
                "..OOCCCCCCCCOO..",
                "..OLO.OOOO.OLO..",
                "..OO........OO..",
                "................"
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(65, 36, 24, 255),
                ['C'] = new Color32(36, 155, 138, 255),
                ['H'] = new Color32(115, 225, 195, 255),
                ['B'] = new Color32(24, 108, 102, 255),
                ['Y'] = new Color32(250, 218, 85, 255),
                ['R'] = new Color32(245, 70, 55, 255),
                ['L'] = new Color32(72, 62, 78, 255),
            };

            return SaveGridSprite(folder, "Spr_Beetle_48", map, pal, 3, ppu: 32f);
        }

        public static Sprite GenerateWindWolfSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(MonstersSheetAbsPath, folder, "Spr_WindWolf_48",
                0.02f, 0.33f, 0.03f, 0.49f, targetWorldHeight: 1.45f);
            if (extracted != null) return extracted;

            string[] map =
            {
                "..OO.......OO...",
                ".OBBO.....OBBO..",
                ".OBMMO...OMMBO..",
                ".OBMMOOOOOMMBO..",
                "..OMMMMMMMMMMO..",
                ".OMMBBBBBBBBMMO.",
                "OMMBBRWBBWRBBMMO",
                ".OOBBBBBBBBBBBO.",
                "..OBBBWWWWBBBBO.",
                "..OBBWWWWWWBBBO.",
                ".OMBBBBBBBBBBMO.",
                "OMMOBBBBBBBOOMMO",
                ".OO.OBBOOBBO.OO.",
                "....OBBOOBBO....",
                "....OOO..OOO....",
                "................"
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(58, 38, 32, 255),
                ['M'] = new Color32(95, 225, 195, 255),
                ['B'] = new Color32(82, 128, 156, 255),
                ['W'] = new Color32(225, 242, 248, 255),
                ['R'] = new Color32(255, 185, 45, 255),
            };

            return SaveGridSprite(folder, "Spr_WindWolf_48", map, pal, 3, ppu: 32f);
        }

        public static Sprite GenerateAncientEntBossSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(MonstersSheetAbsPath, folder, "Spr_EntBoss_128",
                0.66f, 0.99f, 0.02f, 0.57f, targetWorldHeight: 3.20f);
            if (extracted != null) return extracted;

            string[] map =
            {
                "...OOOO..OOOO...",
                ".OOLLLLOOLLLLOO.",
                "OLLLLHHLLLLHHLLO",
                "OLLLLLLLLLLLLLLO",
                ".OOLLLLLLLLLLOO.",
                "..OOWWWWWWWWOO..",
                "..OWWNNWWNNWWO..",
                ".OOWWRWWWRWWWOO.",
                "OWWWWWWCCWWWWWW.",
                "OWWWWWCCCCWWWWWO",
                "OW.WWWCCCCWWW.WO",
                "OO..OWWWWWWO..OO",
                "....OWWWWWWO....",
                "...OWWWWWWWWO...",
                "..OWWWWOOWWWWO..",
                "..OOOOO..OOOOO.."
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(58, 36, 22, 255),
                ['L'] = new Color32(85, 195, 65, 255),
                ['H'] = new Color32(145, 235, 95, 255),
                ['W'] = new Color32(142, 96, 56, 255),
                ['N'] = new Color32(55, 28, 18, 255),
                ['R'] = new Color32(255, 95, 35, 255),
                ['C'] = new Color32(255, 210, 45, 255),
            };

            return SaveGridSprite(folder, "Spr_EntBoss_128", map, pal, 8, ppu: 32f);
        }

        public static Sprite GenerateCampfireSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(PropsSheetAbsPath, folder, "Spr_Campfire_32",
                0.02f, 0.32f, 0.51f, 0.97f, targetWorldHeight: 1.65f);
            if (extracted != null) return extracted;

            string[] map =
            {
                ".......OO.......",
                "......ORRO......",
                ".....ORRRRO.....",
                "....ORROORRO....",
                "...ORRYYYYRRO...",
                "...ORYYYYYYRO...",
                "..ORRYYWWYYRRO..",
                "..ORYYWWWWYYRO..",
                "...OYYYYYYYYO...",
                "..OOWWOOOOOWWOO.",
                ".OWWWWOSSOWWWWWO",
                ".OWWOOSSSSOOWWWO",
                "..OOSSSSSSSSOO..",
                "...OSSSSSSSSO...",
                "....OOOOOOOO....",
                "................"
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(85, 42, 24, 255),
                ['R'] = new Color32(245, 75, 35, 255),
                ['Y'] = new Color32(255, 205, 45, 255),
                ['W'] = new Color32(255, 250, 200, 255),
                ['S'] = new Color32(150, 155, 165, 255),
            };

            return SaveGridSprite(folder, "Spr_Campfire_32", map, pal, 2, ppu: 32f);
        }

        public static Sprite GenerateGlowMushroomSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(PropsSheetAbsPath, folder, "Spr_GlowMushroom_32",
                0.35f, 0.65f, 0.52f, 0.95f, targetWorldHeight: 1.25f, keepSparkles: true);
            if (extracted != null) return extracted;

            string[] map =
            {
                ".....OOOOOO.....",
                "...OOCCCCCCOO...",
                "..OCCWWCCCCCCO..",
                ".OCCCWWCCCCWWCO.",
                ".OCCCCCCCCCCCCO.",
                "OCCCCCCWWCCCCCCO",
                "OBBBBBBBBBBBBBBO",
                ".OO..OSSSSO..OO.",
                "..OOO.OSSO.OOO..",
                ".OCCCO.OO.OCCCO.",
                "OCCWCCO..OCCWCCO",
                ".OCCCCO..OCCCCO.",
                "..OSSO....OSSO..",
                "..OSSO....OSSO..",
                "..OOOO....OOOO..",
                "................"
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(45, 65, 95, 255),
                ['C'] = new Color32(55, 215, 250, 255),
                ['B'] = new Color32(35, 145, 195, 255),
                ['W'] = new Color32(230, 255, 255, 255),
                ['S'] = new Color32(245, 240, 225, 255),
            };

            return SaveGridSprite(folder, "Spr_GlowMushroom_32", map, pal, 2, ppu: 32f);
        }

        /// <summary>
        /// [가디언 테일즈(ref/img) 숲 입구 스타일 최신 픽셀 잔디 바닥]
        /// 256x256 고밀도 픽셀(2x2 도트 블록, Point Filter)로 그린 화사한 투톤 체크 잔디 +
        /// 선명한 픽셀 클로버/풀잎 패턴과 하얀 네잎 꽃송이 군락 타일입니다.
        /// </summary>
        public static Sprite GenerateGrassFloorSprite(string folder)
        {
            string path = $"{folder}/Spr_GrassFloor_64.png";
            int size = 256;
            int cell = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color lightGrass = new Color32(138, 228, 78, 255);  // #8AE44E
            Color lightBevel = new Color32(154, 240, 94, 255);  // 타일 상단/좌측 미세 하이라이트
            Color midGrass = new Color32(122, 214, 68, 255);    // #7AD644
            Color midShadow = new Color32(110, 200, 58, 255);   // 타일 하단/우측 미세 음영
            Color tuftDark = new Color32(84, 174, 48, 255);     // 진한 풀잎 도트
            Color tuftLight = new Color32(168, 246, 108, 255);  // 밝은 풀잎 도트
            Color leafBase = new Color32(72, 162, 46, 255);     // 꽃받침 잎
            Color petalWhite = new Color32(250, 252, 244, 255); // 하얀 꽃잎
            Color petalShade = new Color32(226, 242, 216, 255); // 꽃잎 그림자
            Color flowerCenter = new Color32(252, 204, 52, 255);// 노란 꽃술

            // 1. 2x2 도트 단위의 투톤 체크 잔디 & 타일 베벨 테두리
            for (int y = 0; y < size; y += 2)
            {
                for (int x = 0; x < size; x += 2)
                {
                    int cx = x / cell;
                    int cy = y / cell;
                    int lx = x % cell;
                    int ly = y % cell;
                    bool isLight = (cx + cy) % 2 == 0;

                    Color col = isLight ? lightGrass : midGrass;
                    if (isLight && (lx <= 2 || ly >= cell - 4))
                        col = lightBevel;
                    else if (!isLight && (lx >= cell - 4 || ly <= 2))
                        col = midShadow;

                    SetPixelBlock2x2(tex, size, x, y, col);
                }
            }

            // 2. 선명한 픽셀 클로버/풀잎 패턴 배치
            int[,] tufts = { { 16, 18 }, { 84, 24 }, { 148, 86 }, { 216, 144 }, { 38, 160 }, { 196, 224 }, { 104, 198 }, { 232, 36 } };
            for (int i = 0; i < tufts.GetLength(0); i++)
            {
                int tx = tufts[i, 0];
                int ty = tufts[i, 1];
                SetPixelBlock2x2(tex, size, tx, ty, tuftDark);
                SetPixelBlock2x2(tex, size, tx - 2, ty + 2, tuftDark);
                SetPixelBlock2x2(tex, size, tx + 2, ty + 2, tuftDark);
                SetPixelBlock2x2(tex, size, tx, ty + 2, tuftLight);
                SetPixelBlock2x2(tex, size, tx + 4, ty, tuftLight);
            }

            // 3. 가디언 테일즈풍 하얀 네잎 픽셀 꽃송이 군락
            int[,] flowers = { { 36, 42 }, { 48, 32 }, { 42, 54 }, { 168, 172 }, { 182, 164 }, { 174, 184 }, { 52, 206 }, { 212, 52 } };
            for (int i = 0; i < flowers.GetLength(0); i++)
            {
                int fx = flowers[i, 0];
                int fy = flowers[i, 1];
                SetPixelBlock2x2(tex, size, fx, fy - 4, leafBase);
                SetPixelBlock2x2(tex, size, fx - 4, fy - 2, leafBase);
                SetPixelBlock2x2(tex, size, fx + 4, fy - 2, leafBase);

                SetPixelBlock2x2(tex, size, fx - 2, fy + 2, petalWhite);
                SetPixelBlock2x2(tex, size, fx + 2, fy + 2, petalWhite);
                SetPixelBlock2x2(tex, size, fx - 2, fy - 2, petalShade);
                SetPixelBlock2x2(tex, size, fx + 2, fy - 2, petalShade);
                SetPixelBlock2x2(tex, size, fx, fy, flowerCenter);
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 64f;
                imp.filterMode = FilterMode.Point;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.textureCompression = TextureImporterCompression.Uncompressed;

                TextureImporterSettings settings = new TextureImporterSettings();
                imp.ReadTextureSettings(settings);
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(settings);

                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetPixelBlock2x2(Texture2D tex, int size, int bx, int by, Color col)
        {
            for (int dy = 0; dy < 2; dy++)
            {
                for (int dx = 0; dx < 2; dx++)
                {
                    int px = (bx + dx + size) % size;
                    int py = (by + dy + size) % size;
                    tex.SetPixel(px, py, col);
                }
            }
        }

        /// <summary>
        /// [최신 픽셀 스프라이트] 거대한 황금빛 나무 그루터기
        /// </summary>
        public static Sprite GenerateGiantTreeStumpSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(PropsSheetAbsPath, folder, "Spr_GiantStump_HD",
                0.67f, 0.98f, 0.52f, 0.95f, targetWorldHeight: 2.40f);
            if (extracted != null) return extracted;

            string path = $"{folder}/Spr_GiantStump_HD.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// [최신 픽셀 스프라이트] 가디언 테일즈풍 둥근 숲 나무 프롭
        /// </summary>
        public static Sprite GenerateForestTreeSprite(string folder)
        {
            return TryExtractFromWhiteSheet(PropsSheetAbsPath, folder, "Spr_ForestTree_HD",
                0.02f, 0.32f, 0.02f, 0.49f, targetWorldHeight: 2.80f);
        }

        /// <summary>
        /// [최신 픽셀 스프라이트] 스마트 액션 버튼 기본 공격 대검 아이콘
        /// </summary>
        public static Sprite GenerateAttackSwordIconSprite(string folder)
        {
            return TryExtractFromWhiteSheet(PropsSheetAbsPath, folder, "Spr_Icon_AttackSword_32",
                0.36f, 0.64f, 0.03f, 0.47f, targetWorldHeight: 1.20f);
        }

        /// <summary>
        /// [ref/img 대각선 햇살 샤프트] 화면 상단에서 대각선으로 비치는 가디언 테일즈풍 따스한 햇살(God Rays) 오버레이
        /// </summary>
        public static Sprite GenerateSunbeamSprite(string folder)
        {
            string path = $"{folder}/Spr_Sunbeam_HD.png";
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float diag = Mathf.Abs((u - v * 0.55f) - 0.35f);
                    float beam = Mathf.Clamp01(1f - diag * 4.5f) * Mathf.Clamp01(v * 1.2f);
                    tex.SetPixel(x, y, new Color(1.0f, 1.0f, 0.65f, beam * 0.22f));
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 16f;
                imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Sprite GenerateCarveKnifeIconSprite(string folder)
        {
            Sprite extracted = TryExtractFromWhiteSheet(PropsSheetAbsPath, folder, "Spr_Icon_CarveKnife_32",
                0.70f, 0.96f, 0.03f, 0.47f, targetWorldHeight: 1.20f, keepSparkles: true);
            if (extracted != null) return extracted;

            string[] map =
            {
                "............OOO.",
                "...........OWWWO",
                "..........OWWWWO",
                ".........OWWWWWO",
                "........OWWWWWWO",
                ".......OWWWWWWOO",
                "......OWWWWWOO..",
                ".....OWWWWOO....",
                "...OOOWWOO......",
                "..OGGGOOO.......",
                "...OGGGO........",
                "..OBBBO.........",
                ".OBBBO..........",
                "OBBBO...........",
                "OOOO............",
                "................"
            };

            var pal = new Dictionary<char, Color>
            {
                ['.'] = C_Trans,
                ['O'] = new Color32(75, 38, 24, 255),
                ['W'] = new Color32(235, 245, 255, 255),
                ['G'] = new Color32(245, 195, 65, 255),
                ['B'] = new Color32(145, 80, 45, 255),
            };

            return SaveGridSprite(folder, "Spr_Icon_CarveKnife_32", map, pal, 2, ppu: 32f);
        }

        // =========================================================================
        // [가디언 테일즈 스타일 멀티 파츠 캐릭터 / 무기 / 숲길 프롭 스프라이트]
        // =========================================================================

        public static void GenerateGTHeadSprites(string folder, out Sprite normal, out Sprite blink, out Sprite attack, out Sprite hurt)
        {
            var pal = new Dictionary<char, Color>
            {
                { '.', C_Trans },
                { 'O', new Color32(58, 35, 32, 255) },    // 따뜻한 다크브라운 외곽선
                { 'H', new Color32(126, 74, 50, 255) },   // 체스넛 헤어 기본
                { 'h', new Color32(168, 108, 76, 255) },  // 헤어 하이라이트
                { 'd', new Color32(84, 48, 34, 255) },    // 헤어 그림자
                { 'S', new Color32(255, 218, 182, 255) }, // 복숭아 피부
                { 's', new Color32(238, 178, 142, 255) }, // 턱/볼 그림자
                { 'P', new Color32(246, 138, 136, 255) }, // 볼 홍조
                { 'E', new Color32(38, 25, 46, 255) },    // 깊은 눈동자
                { 'B', new Color32(62, 108, 172, 255) },  // 눈 하단 홍채
                { 'W', Color.white },                     // 눈 하이라이트
                { 'M', new Color32(86, 46, 42, 255) },    // 입선 / 감은 눈
                { 'R', new Color32(204, 72, 78, 255) }    // 기합 입 안쪽
            };

            string[] mapNormal =
            {
                "........................",
                "......OOOOOOOOOOOO......",
                "....OOHHHHHHHHHHHHOO....",
                "...OHHhhhHHHHHHHHhhHO...",
                "..OHhHHHHHHHHHHHHHHhdO..",
                "..OHHHHHHHHHHHHHHHHHdO..",
                ".OHHHdHHHHdddHHHHdHHHHO.",
                ".OHHdHdSSddddddSSdHdHHO.",
                ".OHHdSSSSSSddSSSSSSdHHO.",
                ".OHdSSSSSSSSSSSSSSSSdHO.",
                ".OdSSSWESSSSSSSSWESSSdO.",
                ".OdSSSEESSSSSSSSEESSSdO.",
                ".OsSSSBBSsssssssBBSSSsO.",
                ".OsSPPSSssssssssSSPPssO.",
                "..OsSSSSSSSSMMSSSSSSsO..",
                "..OOssSSSSSSSSSSSSssOO..",
                "....OOsssSSSSSSsssOO....",
                "......OOssssssssOO......",
                "........OOOOOOOO........",
                "........................"
            };

            string[] mapBlink =
            {
                "........................",
                "......OOOOOOOOOOOO......",
                "....OOHHHHHHHHHHHHOO....",
                "...OHHhhhHHHHHHHHhhHO...",
                "..OHhHHHHHHHHHHHHHHhdO..",
                "..OHHHHHHHHHHHHHHHHHdO..",
                ".OHHHdHHHHdddHHHHdHHHHO.",
                ".OHHdHdSSddddddSSdHdHHO.",
                ".OHHdSSSSSSddSSSSSSdHHO.",
                ".OHdSSSSSSSSSSSSSSSSdHO.",
                ".OdSSSSSSSSSSSSSSSSSSdO.",
                ".OdSSSMMSSSSSSSSMMSSSdO.",
                ".OsSSSSSSSSSSSSSSSSSSsO.",
                ".OsSPPSSssssssssSSPPssO.",
                "..OsSSSSSSSSMMSSSSSSsO..",
                "..OOssSSSSSSSSSSSSssOO..",
                "....OOsssSSSSSSsssOO....",
                "......OOssssssssOO......",
                "........OOOOOOOO........",
                "........................"
            };

            string[] mapAttack =
            {
                "........................",
                "......OOOOOOOOOOOO......",
                "....OOHHHHHHHHHHHHOO....",
                "...OHHhhhHHHHHHHHhhHO...",
                "..OHhHHHHHHHHHHHHHHhdO..",
                "..OHHHHHHHHHHHHHHHHHdO..",
                ".OHHHdHHHHdddHHHHdHHHHO.",
                ".OHHdHdSSddddddSSdHdHHO.",
                ".OHHdSSSSSSddSSSSSSdHHO.",
                ".OHdSSOOSSSSSSSSOOSSdHO.",
                ".OdSSSWEOSSSSSSOOWESSdO.",
                ".OdSSSEESSSSSSSSEESSSdO.",
                ".OsSSSBBSsssssssBBSSSsO.",
                ".OsSPPSSsOOOOOOSSSPPssO.",
                "..OsSSSSSORRRROSSSSSsO..",
                "..OOssSSSSOOOOSsssSsOO..",
                "....OOsssSSSSSSsssOO....",
                "......OOssssssssOO......",
                "........OOOOOOOO........",
                "........................"
            };

            string[] mapHurt =
            {
                "........................",
                "......OOOOOOOOOOOO......",
                "....OOHHHHHHHHHHHHOO....",
                "...OHHhhhHHHHHHHHhhHO...",
                "..OHhHHHHHHHHHHHHHHhdO..",
                "..OHHHHHHHHHHHHHHHHHdO..",
                ".OHHHdHHHHdddHHHHdHHHHO.",
                ".OHHdHdSSddddddSSdHdHHO.",
                ".OHHdSSSSSSddSSSSSSdHHO.",
                ".OHdSSSSSSSSSSSSSSSSdHO.",
                ".OdSSSMSSSSSSSSSSMSSSdO.",
                ".OdSSSSMMSSSSSSMMSSSSdO.",
                ".OsSSSMSSSSSSSSSSMSSSsO.",
                ".OsSPPSSssssssssSSPPssO.",
                "..OsSSSSSSOOOSSSSSSSsO..",
                "..OOssSSSSOROSSSSSssOO..",
                "....OOsssSSSSSSsssOO....",
                "......OOssssssssOO......",
                "........OOOOOOOO........",
                "........................"
            };

            normal = SaveGridSprite(folder, "Spr_GT_Head_Normal", mapNormal, pal, 2, ppu: 64f);
            blink = SaveGridSprite(folder, "Spr_GT_Head_Blink", mapBlink, pal, 2, ppu: 64f);
            attack = SaveGridSprite(folder, "Spr_GT_Head_Attack", mapAttack, pal, 2, ppu: 64f);
            hurt = SaveGridSprite(folder, "Spr_GT_Head_Hurt", mapHurt, pal, 2, ppu: 64f);
        }

        public static void GenerateGTClassHeadgearSprites(string folder, out Sprite warriorHelm, out Sprite scouterHood, out Sprite guardianKnightHelm)
        {
            var pal = new Dictionary<char, Color>
            {
                { '.', C_Trans },
                { 'O', new Color32(48, 34, 42, 255) },    // 외곽선
                { 'W', new Color32(236, 240, 248, 255) }, // 화이트 아머/바이저
                { 'w', new Color32(190, 202, 220, 255) }, // 화이트 그림자
                { 'R', new Color32(224, 58, 66, 255) },   // 가디언즈 붉은 깃털 장식
                { 'r', new Color32(166, 36, 46, 255) },   // 붉은 깃털 음영
                { 'G', new Color32(246, 198, 72, 255) },  // 금장 테두리
                { 'g', new Color32(196, 144, 42, 255) },  // 금장 그림자
                { 'S', new Color32(118, 152, 198, 255) }, // 워리어 강철 투구
                { 's', new Color32(78, 106, 148, 255) },  // 강철 음영
                { 'E', new Color32(72, 166, 94, 255) },   // 스카우터 에메랄드 후드
                { 'e', new Color32(48, 118, 66, 255) }    // 후드 음영
            };

            string[] mapWarrior =
            {
                "..........OOOOOO..........",
                ".........ORRRRRRO.........",
                "........ORRRRRrrRO........",
                "......OOOGGGGGGGGOOO......",
                "....OOSSSSSSSSSSSSSSOO....",
                "...OSSSSSSSSSSSSSSSSSSO...",
                "..OOSSSWWSSSSSSSSWWSSSOO..",
                ".OWWOSSWWSSSSSSSSWWSSOOWO.",
                ".OWWOSsSSSSSSSSSSSSsSOWWO.",
                "..OOSSOGGGGGGGGGGGGOSSOO..",
                "..OSssO............OssSO..",
                "..OSsO..............OsSO..",
                "..OSsO..............OsSO..",
                "..OSSO..............OSSO..",
                "...OO................OO...",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                ".........................."
            };

            string[] mapScouter =
            {
                "..........OOOOOO..........",
                "........OOEEEEEEOO........",
                ".......OEEEEEEEEEEO.......",
                ".....OOEEEEGGGGEEEEOO.....",
                "....OEEEEEEeeeeEEEEEEO....",
                "...OEEEEEEEEEEEEEEEEEEO...",
                "..OEEEEeeeeeeeeeeeeEEEEO..",
                "..OEEeO............OeEEO..",
                "..OEEO..............OEEO..",
                "..OEEO..............OEEO..",
                "..OEEO..............OEEO..",
                "..OeeO..............OeeO..",
                "...OO................OO...",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                ".........................."
            };

            string[] mapGuardian =
            {
                ".........OOOOOOOO.........",
                "........ORRRRRRRRO........",
                ".......ORRRRRRRRRrO.......",
                ".......ORRRrrrrrrRO.......",
                "......OOOGGGGGGGGOOO......",
                "....OOWWWWWWWWWWWWWWOO....",
                "...OWWWWWWWWWWWWWWWWWWO...",
                "..OWWWWWwwwwwwwwwwWWWWWO..",
                "..OWWWOGGGGGGGGGGGGOWWWO..",
                "..OWwWO............OWwWO..",
                "..OWwO..............OwWO..",
                "..OWwO..............OwWO..",
                "..OWwO..............OwWO..",
                "...OOO..............OOO...",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                "..........................",
                ".........................."
            };

            warriorHelm = SaveGridSprite(folder, "Spr_GT_Helm_Warrior", mapWarrior, pal, 2, ppu: 64f);
            scouterHood = SaveGridSprite(folder, "Spr_GT_Helm_Scouter", mapScouter, pal, 2, ppu: 64f);
            guardianKnightHelm = SaveGridSprite(folder, "Spr_GT_Helm_Guardian", mapGuardian, pal, 2, ppu: 64f);
        }

        public static void GenerateGTBodyAndLimbSprites(
            string folder,
            out Sprite bodyArmor,
            out Sprite footSprite,
            out Sprite handSprite,
            out Sprite shieldSprite,
            out Sprite shadowSprite,
            out Sprite dustPuffSprite)
        {
            var pal = new Dictionary<char, Color>
            {
                { '.', C_Trans },
                { 'O', new Color32(52, 34, 34, 255) },
                { 'A', new Color32(214, 224, 238, 255) }, // 은백색 갑옷
                { 'a', new Color32(154, 170, 194, 255) }, // 갑옷 음영
                { 'B', new Color32(56, 98, 168, 255) },   // 로열 블루 튜닉/방패
                { 'b', new Color32(38, 66, 122, 255) },   // 블루 음영
                { 'R', new Color32(214, 58, 64, 255) },   // 붉은 망토/스카프
                { 'G', new Color32(244, 194, 68, 255) },  // 금장 버클/테두리
                { 'L', new Color32(128, 76, 48, 255) },   // 가죽 벨트/부츠
                { 'l', new Color32(88, 50, 32, 255) },    // 가죽 음영
                { 'S', new Color32(255, 218, 182, 255) }, // 손 피부/글러브
                { 'D', new Color32(24, 34, 42, 115) },    // 반투명 발 그림자
                { 'd', new Color32(24, 34, 42, 65) },     // 외곽 연한 그림자
                { 'C', new Color32(252, 248, 236, 235) }, // 먼지 구름 밝은색
                { 'c', new Color32(226, 216, 192, 185) }  // 먼지 구름 음영
            };

            string[] mapBody =
            {
                "....OOOOOOOOOO....",
                "...ORRRRRRRRRRO...",
                "..OOAAAAAAAAAOO...",
                "..OAAAABBBBAAAAO..",
                ".OAAABBBBBBBBAAAO.",
                ".OAaABBBBBBBBaAAO.",
                ".OAaAABBBBBBaAAAO.",
                ".OLLLLGGGGGGLLLLLO",
                "..OlllGGGGGGlllO..",
                "..OBBBBBBBBBBBBO..",
                "..ObbbbbbbbbbbbO..",
                "...ObbO....ObbO...",
                "....OO......OO....",
                ".................."
            };

            string[] mapFoot =
            {
                "..OOOO..",
                ".OLLLLLO",
                "OLLlLLlO",
                "OLllllLO",
                ".OOOOOO.",
                "........",
                "........"
            };

            string[] mapHand =
            {
                ".OOOO.",
                "OSSSSO",
                "OSssSO",
                "OSSssO",
                ".OOOO.",
                "......"
            };
            pal['s'] = new Color32(228, 174, 138, 255);

            string[] mapShield =
            {
                "..OOOOOOOO..",
                ".OGGGGGGGGO.",
                "OGBBBBBBBBGO",
                "OGBAAAAAAbGO",
                "OGBAAGGAAbGO",
                "OGBAAAAAAbGO",
                "OGBBAAAbbBGO",
                "OGBBBBBBBBGO",
                ".OGbbbbbbGO.",
                ".OGGbbbbGGO.",
                "..OGGGGGGO..",
                "...OOGGOO...",
                ".....OO.....",
                "............"
            };

            string[] mapShadow =
            {
                "......dddddddddddd......",
                "...dddDDDDDDDDDDDDddd...",
                ".ddDDDDDDDDDDDDDDDDDDdd.",
                "dDDDDDDDDDDDDDDDDDDDDDDd",
                "dDDDDDDDDDDDDDDDDDDDDDDd",
                ".ddDDDDDDDDDDDDDDDDDDdd.",
                "...dddDDDDDDDDDDDDddd...",
                "......dddddddddddd......",
                "........................",
                "........................"
            };

            string[] mapDust =
            {
                "....CCCC....",
                "..CCCCCCCC..",
                ".CCCCCCCCCc.",
                "CCCCCCCCCCcc",
                "CCCcCCCCcccc",
                ".cccccccccc.",
                "..cccccccc..",
                "............",
                "............",
                "............"
            };

            bodyArmor = SaveGridSprite(folder, "Spr_GT_Body", mapBody, pal, 2, ppu: 64f);
            footSprite = SaveGridSprite(folder, "Spr_GT_Foot", mapFoot, pal, 2, ppu: 64f);
            handSprite = SaveGridSprite(folder, "Spr_GT_Hand", mapHand, pal, 2, ppu: 64f);
            shieldSprite = SaveGridSprite(folder, "Spr_GT_Shield", mapShield, pal, 2, ppu: 64f);
            shadowSprite = SaveGridSprite(folder, "Spr_GT_Shadow", mapShadow, pal, 2, ppu: 64f);
            dustPuffSprite = SaveGridSprite(folder, "Spr_GT_DustPuff", mapDust, pal, 2, ppu: 48f);
        }

        public static void GenerateGTClassWeaponSprites(
            string folder,
            out Sprite warriorGreatsword,
            out Sprite scouterDagger,
            out Sprite guardianMace)
        {
            var pal = new Dictionary<char, Color>
            {
                { '.', C_Trans },
                { 'O', new Color32(42, 32, 44, 255) },
                { 'W', new Color32(246, 250, 255, 255) }, // 검날 하이라이트
                { 'S', new Color32(182, 206, 232, 255) }, // 강철 검날
                { 's', new Color32(118, 146, 182, 255) }, // 검날 음영
                { 'G', new Color32(246, 198, 72, 255) },  // 금장 크로스가드/해머테
                { 'g', new Color32(192, 138, 38, 255) },  // 금장 음영
                { 'L', new Color32(124, 72, 46, 255) },   // 가죽 손잡이
                { 'R', new Color32(220, 56, 68, 255) },   // 루비 보석
                { 'E', new Color32(82, 218, 128, 255) },  // 스카우터 에메랄드 날
                { 'e', new Color32(44, 148, 86, 255) },   // 에메랄드 음영
                { 'C', new Color32(84, 212, 255, 255) }   // 성기사 마력 코어
            };

            string[] mapWarriorSword =
            {
                "......OO......",
                ".....OWSO.....",
                "....OWWSSO....",
                "....OWWSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                "...OWWSSSsO...",
                ".OOGGGGGGGGOO.",
                "OGGGGGGGGGGGGO",
                "OgGGGGORROGGGg",
                ".OOOOLLLLOOOO.",
                ".....OLLO.....",
                ".....OLLO.....",
                ".....OLLO.....",
                "....OGGGGO....",
                ".....OOOO.....",
                "..............",
                ".............."
            };

            string[] mapScouterBlade =
            {
                "....OO....",
                "...OEEO...",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                "..OWEEeO..",
                ".OOGGGGOO.",
                "OGGGGGGGGO",
                ".OOOLLOOO.",
                "...OLLO...",
                "...OLLO...",
                "...OGGO...",
                "....OO....",
                "..........",
                "..........",
                ".........."
            };

            string[] mapGuardianMace =
            {
                "....OOOOOOOO....",
                "..OOGGGGGGGGOO..",
                ".OGGWWCCCCWWGGO.",
                "OGGWCCCCCCCCWGGO",
                "OGSWCCCCCCCCWSGO",
                "OGSWCCCCCCCCWSGO",
                "OGSsGGCCCCGGsSGO",
                ".OGGGGGGGGGGGGO.",
                "..OOGGSSSSGGOO..",
                "....OOGGGGOO....",
                "......OSSO......",
                "......OSSO......",
                "......OSSO......",
                "......OSSO......",
                "......OSSO......",
                "......OSSO......",
                "......OSSO......",
                "......OLLO......",
                "......OLLO......",
                "......OLLO......",
                ".....OGGGGO.....",
                "......OOOO......",
                "................",
                "................",
                "................",
                "................"
            };

            Vector2 hiltPivot = new Vector2(0.5f, 0.14f);
            warriorGreatsword = SaveGridSprite(folder, "Spr_GT_Weapon_Warrior", mapWarriorSword, pal, 2, ppu: 48f, customPivot: hiltPivot);
            scouterDagger = SaveGridSprite(folder, "Spr_GT_Weapon_Scouter", mapScouterBlade, pal, 2, ppu: 48f, customPivot: hiltPivot);
            guardianMace = SaveGridSprite(folder, "Spr_GT_Weapon_Guardian", mapGuardianMace, pal, 2, ppu: 48f, customPivot: hiltPivot);
        }

        public static Sprite GenerateDirtRoadSprite(string folder)
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color dirtBase = new Color32(214, 166, 108, 255);
            Color dirtLight = new Color32(230, 188, 132, 255);
            Color dirtDark = new Color32(182, 134, 82, 255);
            Color edgeGrass = new Color32(124, 186, 68, 255);
            Color pebble = new Color32(156, 114, 72, 255);

            for (int y = 0; y < size; y += 2)
            {
                for (int x = 0; x < size; x += 2)
                {
                    float edgeDist = Mathf.Min(y, size - 2 - y) / 10f;
                    Color c;
                    if (edgeDist < 0.25f)
                    {
                        c = edgeGrass;
                    }
                    else if (edgeDist < 0.5f && ((x / 2 + y / 2) % 3 == 0))
                    {
                        c = edgeGrass;
                    }
                    else
                    {
                        int h = ((x * 37 + y * 91) ^ (x * 13)) & 15;
                        c = h == 0 ? pebble : (h < 4 ? dirtDark : (h > 12 ? dirtLight : dirtBase));
                    }

                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                            tex.SetPixel(x + sx, y + sy, c);
                }
            }

            tex.Apply();
            string path = $"{folder}/Spr_DirtRoad_64.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                TextureImporterSettings tis = new TextureImporterSettings();
                importer.ReadTextureSettings(tis);
                tis.spriteMode = (int)SpriteImportMode.Single;
                importer.SetTextureSettings(tis);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Sprite GenerateStoneSlabSprite(string folder)
        {
            var pal = new Dictionary<char, Color>
            {
                { '.', C_Trans },
                { 'O', new Color32(58, 66, 76, 255) },    // 외곽선
                { 'L', new Color32(198, 208, 218, 255) }, // 햇빛 받은 상단 모서리
                { 'S', new Color32(154, 166, 178, 255) }, // 석판 주 색상
                { 's', new Color32(114, 126, 140, 255) }, // 측면 베벨 음영
                { 'C', new Color32(84, 94, 108, 255) },   // 균열선
                { 'M', new Color32(116, 184, 78, 255) },  // 밝은 이끼
                { 'm', new Color32(78, 138, 52, 255) },   // 어두운 이끼
                { 'D', new Color32(28, 42, 32, 90) }      // 지면 그림자
            };

            string[] map =
            {
                "........................",
                "....OOOOOOOOOOOOOO......",
                "...OLLLLLLLLLLLLLLO.....",
                "..OLSSSSSSSSSSSSSSLO....",
                "..OSSSSSSSSSSCSSSSSO....",
                "..OSSSSMMmSSCCSSSSSO....",
                "..OSSSMMMMSSCSSSSSSO....",
                "..OSSSSmmSSSSSSSSSSO....",
                "..OSSSSSSSSSSSSSSSSO....",
                "..OSSSSSSSSSCSSSSSSO....",
                "..OSSSSSSSSCCSSSSSSO....",
                "..OSSSSSSSSSSSSSSSSO....",
                "..OssssssssssssssssO....",
                "..OssssssssssssssssO....",
                "..OOOOOOOOOOOOOOOOOO....",
                "...DDDDDDDDDDDDDDDD.....",
                "....DDDDDDDDDDDDDD......",
                "........................",
                "........................",
                "........................",
                "........................",
                "........................",
                "........................",
                "........................"
            };

            return SaveGridSprite(folder, "Spr_StoneSlab_48", map, pal, 2, ppu: 32f);
        }

        private static void DrawSoftCircle(Texture2D tex, int texSize, int cx, int cy, float radius, Color col)
        {
            int rInt = Mathf.CeilToInt(radius + 1f);
            for (int dy = -rInt; dy <= rInt; dy++)
            {
                for (int dx = -rInt; dx <= rInt; dx++)
                {
                    int px = (cx + dx + texSize) % texSize;
                    int py = (cy + dy + texSize) % texSize;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= radius)
                    {
                        float t = Mathf.Clamp01(radius - d + 0.5f);
                        Color prev = tex.GetPixel(px, py);
                        tex.SetPixel(px, py, Color.Lerp(prev, col, t));
                    }
                }
            }
        }

        private static Sprite SaveGridSprite(string folder, string name, string[] rows,
            Dictionary<char, Color> palette, int scale, float ppu = 32f, Vector2? customPivot = null)
        {
            int gridH = rows.Length;
            int gridW = rows[0].Length;
            int texW = gridW * scale;
            int texH = gridH * scale;

            Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);

            for (int gy = 0; gy < gridH; gy++)
            {
                string row = rows[gridH - 1 - gy];
                for (int gx = 0; gx < gridW; gx++)
                {
                    char c = gx < row.Length ? row[gx] : '.';
                    Color col = palette.TryGetValue(c, out Color found) ? found : C_Trans;

                    for (int sy = 0; sy < scale; sy++)
                    {
                        for (int sx = 0; sx < scale; sx++)
                        {
                            tex.SetPixel(gx * scale + sx, gy * scale + sy, col);
                        }
                    }
                }
            }

            tex.Apply();
            string path = $"{folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                TextureImporterSettings tis = new TextureImporterSettings();
                importer.ReadTextureSettings(tis);
                tis.spriteMode = (int)SpriteImportMode.Single;
                if (customPivot.HasValue)
                {
                    tis.spriteAlignment = (int)SpriteAlignment.Custom;
                    tis.spritePivot = customPivot.Value;
                }
                importer.SetTextureSettings(tis);

                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
