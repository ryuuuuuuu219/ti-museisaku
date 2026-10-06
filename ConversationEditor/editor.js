const saveButton = document.getElementById("saveButton");
const openButton = document.getElementById("openButton");
const fileInput = document.getElementById("fileInput");
const statusText = document.getElementById("statusText");
const nodeArea = document.getElementById("nodeArea");
const connectionLayer = document.getElementById("connectionLayer");

const X_STEP = 200;
const Y_STEP = 300;
const NODE_WIDTH = 180;
const NODE_HEIGHT = 220;

let currentSourceText = "";

saveButton.addEventListener("click", function () {
    if (!currentSourceText) {
        alert("保存する会話データがありません");
        return;
    }

    const blob = new Blob([currentSourceText], { type: "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "input.txt";
    link.click();
    URL.revokeObjectURL(url);
});

openButton.addEventListener("click", function () {
    fileInput.click();
});

fileInput.addEventListener("change", async function () {
    const file = fileInput.files[0];
    if (!file) {
        return;
    }

    loadConversation(await file.text(), file.name);
});

function loadConversation(sourceText, sourceName) {
    const nodes = parseConversation(sourceText);
    currentSourceText = sourceText;

    if (nodes.length === 0) {
        nodeArea.replaceChildren(connectionLayer);
        statusText.textContent = `${sourceName}: ノードを読み取れませんでした`;
        return;
    }

    renderNodes(nodes);
    statusText.textContent = `${sourceName}: ${nodes.length}ノード`;
}

function parseConversation(sourceText) {
    const nodes = [];
    const blocks = sourceText.split(/(?=^\[[^\]\r\n]+\]\s*$)/gm);

    for (const block of blocks) {
        const headerMatch = block.match(/^\[([^\]\r\n]+)\]\s*\r?\n?/);
        if (!headerMatch) {
            continue;
        }

        const fields = parseFields(block.slice(headerMatch[0].length));
        nodes.push({
            id: headerMatch[1].trim(),
            expectedText: fields.get("想定文") || fields.get("質問例") || fields.get("想定入力") || "",
            keywords: fields.get("キーワード") || "—",
            response: fields.get("返答") || "",
            prerequisiteIds: extractNodeIds(fields.get("前提ノード")),
            interlockingIds: extractNodeIds(
                fields.get("インターロッキングノード") || fields.get("除外ノード")
            )
        });
    }

    return nodes;
}

function parseFields(blockText) {
    const fields = new Map();
    let currentKey = null;

    for (const rawLine of blockText.split(/\r?\n/)) {
        const fieldMatch = rawLine.match(/^([^:：]+)[:：](.*)$/);
        if (fieldMatch) {
            currentKey = fieldMatch[1].trim();
            fields.set(currentKey, fieldMatch[2].trim());
        } else if (currentKey && rawLine.trim()) {
            fields.set(currentKey, `${fields.get(currentKey)}\n${rawLine.trim()}`);
        }
    }

    return fields;
}

function extractNodeIds(value = "") {
    return value.match(/S\d+-C\d+/g) || [];
}

function calculatePositions(nodes) {
    const positions = new Map();
    const nodesById = new Map(nodes.map((node) => [node.id, node]));
    const visiting = new Set();

    function placeNode(node) {
        if (positions.has(node.id)) {
            return positions.get(node.id);
        }

        if (visiting.has(node.id)) {
            return placeAtBottom(node.id);
        }

        visiting.add(node.id);
        const prerequisite = node.prerequisiteIds
            .map((id) => nodesById.get(id))
            .find(Boolean);

        let position;
        if (prerequisite) {
            const prerequisitePosition = placeNode(prerequisite);
            position = {
                x: prerequisitePosition.x + X_STEP,
                y: prerequisitePosition.y
            };
            positions.set(node.id, position);
        } else {
            position = placeAtBottom(node.id);
        }

        visiting.delete(node.id);
        return position;
    }

    function placeAtBottom(nodeId) {
        const lowestY = positions.size === 0
            ? -Y_STEP
            : Math.max(...Array.from(positions.values(), (position) => position.y));
        const position = { x: 0, y: lowestY + Y_STEP };
        positions.set(nodeId, position);
        return position;
    }

    nodes.forEach(placeNode);
    return positions;
}

function renderNodes(nodes) {
    const positions = calculatePositions(nodes);
    const nodesById = new Map(nodes.map((node) => [node.id, node]));
    const incomingInterlockingIds = createIncomingInterlockingMap(nodes);
    const fragment = document.createDocumentFragment();

    nodeArea.replaceChildren(connectionLayer);

    for (const nodeData of nodes) {
        const position = positions.get(nodeData.id);
        const nodeElement = document.createElement("article");
        nodeElement.className = "conversation-node";
        nodeElement.id = `node-${nodeData.id}`;
        nodeElement.style.left = `${position.x}px`;
        nodeElement.style.top = `${position.y}px`;

        const title = document.createElement("h2");
        title.textContent = nodeData.id;
        nodeElement.appendChild(title);

        const interlockingIds = Array.from(new Set([
            ...nodeData.interlockingIds,
            ...(incomingInterlockingIds.get(nodeData.id) || [])
        ]));
        if (interlockingIds.length > 0) {
            const links = document.createElement("div");
            links.className = "interlocking-links";
            links.setAttribute("aria-label", "インターロッキングノード");

            for (const interlockingId of interlockingIds) {
                const link = document.createElement("a");
                link.className = "interlocking-link";
                link.href = `#node-${interlockingId}`;
                link.textContent = `[${interlockingId}]`;
                link.title = `インターロッキングノード ${interlockingId} へ移動`;
                links.appendChild(link);
            }

            nodeElement.appendChild(links);
        }

        appendField(nodeElement, "想定文", nodeData.expectedText || "—");
        appendField(nodeElement, "キーワード", nodeData.keywords);
        appendField(nodeElement, "返答", nodeData.response || "—");

        if (nodeData.prerequisiteIds.length > 0) {
            appendField(nodeElement, "前提", nodeData.prerequisiteIds.join("、"));
        }

        fragment.appendChild(nodeElement);
    }

    nodeArea.appendChild(fragment);

    const maxX = Math.max(...Array.from(positions.values(), (position) => position.x));
    const maxY = Math.max(...Array.from(positions.values(), (position) => position.y));
    const canvasWidth = maxX + NODE_WIDTH + 80;
    const canvasHeight = maxY + NODE_HEIGHT + 80;
    nodeArea.style.width = `${canvasWidth}px`;
    nodeArea.style.height = `${canvasHeight}px`;
    connectionLayer.setAttribute("width", canvasWidth);
    connectionLayer.setAttribute("height", canvasHeight);
    connectionLayer.setAttribute("viewBox", `0 0 ${canvasWidth} ${canvasHeight}`);

    drawConnections(nodes, nodesById, positions);
}

function createIncomingInterlockingMap(nodes) {
    const incomingIds = new Map();

    for (const node of nodes) {
        for (const targetId of node.interlockingIds) {
            if (!incomingIds.has(targetId)) {
                incomingIds.set(targetId, []);
            }
            incomingIds.get(targetId).push(node.id);
        }
    }

    return incomingIds;
}

function appendField(nodeElement, label, value) {
    if (!value) {
        return;
    }

    const field = document.createElement("p");
    const labelElement = document.createElement("strong");
    labelElement.textContent = `${label}: `;
    field.append(labelElement, document.createTextNode(value.replaceAll("\\n", "\n")));
    nodeElement.appendChild(field);
}

function drawConnections(nodes, nodesById, positions) {
    connectionLayer.replaceChildren();

    for (const node of nodes) {
        const target = positions.get(node.id);
        for (const prerequisiteId of node.prerequisiteIds) {
            if (!nodesById.has(prerequisiteId)) {
                continue;
            }

            const source = positions.get(prerequisiteId);
            const line = document.createElementNS("http://www.w3.org/2000/svg", "path");
            const startX = source.x + NODE_WIDTH;
            const startY = source.y + NODE_HEIGHT / 2;
            const endX = target.x;
            const endY = target.y + NODE_HEIGHT / 2;
            const controlOffset = Math.max(40, Math.abs(endX - startX) / 2);

            line.setAttribute(
                "d",
                `M ${startX} ${startY} C ${startX + controlOffset} ${startY}, ${endX - controlOffset} ${endY}, ${endX} ${endY}`
            );
            line.setAttribute("class", "prerequisite-connection");
            connectionLayer.appendChild(line);
        }
    }
}

const bundledInput = window.CONVERSATION_INPUT || "";

if (bundledInput) {
    loadConversation(bundledInput, "input.txt（同梱）");
}

fetch("input.txt", { cache: "no-store" })
    .then((response) => {
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        return response.text();
    })
    .then((sourceText) => loadConversation(sourceText, "input.txt"))
    .catch(() => {
        if (!bundledInput) {
            statusText.textContent = "input.txtを開いてください";
        }
    });
