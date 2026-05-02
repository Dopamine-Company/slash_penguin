/**
 * Notion 페이지 최상위 블록을 UTF-8로 다시 씁니다 (PowerShell 인코딩 깨짐 복구용).
 * 사용: NOTION_TOKEN=... node tools/notion-rewrite-blocks.mjs
 */
const PAGE_ID = "3549f69f-2f89-80ab-98f0-df60c70cef8d";
const NOTION_VERSION = "2022-06-28";

const token = process.env.NOTION_TOKEN;
if (!token) {
  console.error("NOTION_TOKEN 환경 변수를 설정하세요.");
  process.exit(1);
}

function headers(extra = {}) {
  return {
    Authorization: `Bearer ${token}`,
    "Notion-Version": NOTION_VERSION,
    "Content-Type": "application/json; charset=utf-8",
    ...extra,
  };
}

async function notionFetch(path, { method = "GET", body } = {}) {
  const res = await fetch(`https://api.notion.com${path}`, {
    method,
    headers: headers(),
    body: body != null ? Buffer.from(body, "utf8") : undefined,
  });
  const text = await res.text();
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = { raw: text };
  }
  if (!res.ok) {
    throw new Error(`${method} ${path} ${res.status}: ${text.slice(0, 500)}`);
  }
  return json;
}

async function listBlockChildren(blockId) {
  const out = [];
  let cursor = undefined;
  do {
    const q = cursor ? `?start_cursor=${cursor}&page_size=100` : "?page_size=100";
    const data = await notionFetch(`/v1/blocks/${blockId}/children${q}`);
    out.push(...data.results);
    cursor = data.has_more ? data.next_cursor : undefined;
  } while (cursor);
  return out;
}

async function archiveBlock(blockId) {
  await notionFetch(`/v1/blocks/${blockId}`, {
    method: "PATCH",
    body: JSON.stringify({ archived: true }),
  });
}

async function deleteBlockTree(blockId) {
  const block = await notionFetch(`/v1/blocks/${blockId}`);
  if (block.has_children) {
    const kids = await listBlockChildren(blockId);
    for (const k of kids) {
      await deleteBlockTree(k.id);
    }
  }
  await archiveBlock(blockId);
}

function t(content) {
  return [{ type: "text", text: { content } }];
}

const children = [
  {
    object: "block",
    type: "paragraph",
    paragraph: {
      rich_text: t(
        "[slash_penguin] 게임잼 스타일 기획·AI 활용 계획 (UTF-8 재기입). 아래 섹션을 기준으로 구현 단계를 나눕니다."
      ),
    },
  },
  { object: "block", type: "divider", divider: {} },
  {
    object: "block",
    type: "heading_1",
    heading_1: { rich_text: t("기획") },
  },
  {
    object: "block",
    type: "paragraph",
    paragraph: {
      rich_text: t(
        "Unity 3D 모바일 원터치. 펭귄 엉덩이 타격 코어 루프 (플로우 번호)."
      ),
    },
  },
  {
    object: "block",
    type: "numbered_list_item",
    numbered_list_item: {
      rich_text: t(
        "시작 대기: 앱 실행 시 온보딩 없이 펭귄 3D 메시가 뒤를 보는 장면."
      ),
    },
  },
  {
    object: "block",
    type: "numbered_list_item",
    numbered_list_item: {
      rich_text: t(
        "시작 트리거: 펭귄에 겹친 팬티 터치 후 아래로 스와이프 → 스와이프 Y에 맞춰 팬티가 벗겨짐."
      ),
    },
  },
  {
    object: "block",
    type: "numbered_list_item",
    numbered_list_item: {
      rich_text: t(
        "게임 루프: 시작 후 왼쪽·오른쪽 엉덩이가 랜덤으로 커졌다 작아짐 (터치 유도). 가만히 있는 쪽은 타격 금지."
      ),
    },
  },
  {
    object: "block",
    type: "numbered_list_item",
    numbered_list_item: {
      rich_text: t(
        "규칙: 활성(신호) 볼기짝을 스와이프하면 엉덩이가 점점 빨개짐(redness 증가)."
      ),
    },
  },
  {
    object: "block",
    type: "numbered_list_item",
    numbered_list_item: {
      rich_text: t(
        "규칙: 비활성 볼기짝을 스와이프하면 방귀, redness 감소."
      ),
    },
  },
  {
    object: "block",
    type: "numbered_list_item",
    numbered_list_item: {
      rich_text: t(
        "성공 엔딩: 완전히 빨개지면 엉덩이가 조명처럼 빛남(이미션·텍스처)."
      ),
    },
  },
  {
    object: "block",
    type: "numbered_list_item",
    numbered_list_item: {
      rich_text: t(
        "실패 엔딩: 방귀 누적 4회째에는 방귀 대신 하얀 똥이 화면 전체를 페인트처럼 덮으며 종료."
      ),
    },
  },
  {
    object: "block",
    type: "numbered_list_item",
    numbered_list_item: {
      rich_text: t("재시작: 엔딩 후 화면 한 번 더 터치하면 1번으로 복귀."),
    },
  },
  { object: "block", type: "divider", divider: {} },
  {
    object: "block",
    type: "heading_1",
    heading_1: { rich_text: t("AI 활용 계획") },
  },
  {
    object: "block",
    type: "paragraph",
    paragraph: {
      rich_text: t(
        "런타임 AI(ML 난이도 등)는 필수 아님. 룰 기반 + 개발 생산성용 AI(Cursor·Claude, Codex, 생성형 에셋) 중심."
      ),
    },
  },
  {
    object: "block",
    type: "heading_2",
    heading_2: { rich_text: t("Step 1 — 상태머신 분해") },
  },
  {
    object: "block",
    type: "bulleted_list_item",
    bulleted_list_item: {
      rich_text: t(
        "GameState 예시: WaitingStart, PullingPanty, Playing, SuccessEnd, PoopEnd."
      ),
    },
  },
  {
    object: "block",
    type: "bulleted_list_item",
    bulleted_list_item: {
      rich_text: t(
        "상태별 Enter / Update / Exit로 쪼개서, AI에게 한 번에 전체 게임이 아니라 상태 단위로 시킨다."
      ),
    },
  },
  {
    object: "block",
    type: "heading_2",
    heading_2: { rich_text: t("Step 2 — 흰박스 프로토타입") },
  },
  {
    object: "block",
    type: "bulleted_list_item",
    bulleted_list_item: {
      rich_text: t(
        "펭귄=캡슐, 좌·우 엉덩이=구, 팬티=얇은 큐브, 똥 연출=UI Image 등 프리미티브로 먼저 루프 검증."
      ),
    },
  },
  {
    object: "block",
    type: "bulleted_list_item",
    bulleted_list_item: {
      rich_text: t(
        "DOTween 가정, 없으면 코루틴 폴백을 명시한 단일 프롬프트로 플레이 가능 빌드부터."
      ),
    },
  },
  {
    object: "block",
    type: "heading_2",
    heading_2: { rich_text: t("Step 3 — 에셋") },
  },
  {
    object: "block",
    type: "bulleted_list_item",
    bulleted_list_item: {
      rich_text: t(
        "메시·텍스처·파티클·사운드는 생성 AI·스토어·블렌더로 순차 교체. 첫판은 예쁨보다 타격감·타이밍 우선."
      ),
    },
  },
  {
    object: "block",
    type: "heading_2",
    heading_2: { rich_text: t("Step 4 — ScriptableObject 튜닝") },
  },
  {
    object: "block",
    type: "bulleted_list_item",
    bulleted_list_item: {
      rich_text: t(
        "스와이프 거리, redness 증감, fart 임계, DOTween·Feel 수치를 SO로 분리해 AI·인간이 반복 튜닝."
      ),
    },
  },
  {
    object: "block",
    type: "paragraph",
    paragraph: {
      rich_text: t(
        "메시 권장: 몸통·좌·우 볼기짝 분리 + DOTween·Feel + 필요 시 BlendShape 보조 (한 덩어리 애니만으로는 스와이프 방향 탄성 연출이 어려울 수 있음)."
      ),
    },
  },
];

async function appendChildren(blockId, chunk) {
  const body = JSON.stringify({ children: chunk });
  return notionFetch(`/v1/blocks/${blockId}/children`, {
    method: "PATCH",
    body,
  });
}

async function main() {
  const top = await listBlockChildren(PAGE_ID);
  console.log(`기존 최상위 블록 ${top.length}개 아카이브 중…`);
  for (const b of top) {
    await deleteBlockTree(b.id);
  }
  const batch = 100;
  for (let i = 0; i < children.length; i += batch) {
    const chunk = children.slice(i, i + batch);
    console.log(`블록 ${i + 1}~${i + chunk.length} 추가…`);
    await appendChildren(PAGE_ID, chunk);
  }
  console.log("완료. 노션에서 새로고침하세요.");
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
