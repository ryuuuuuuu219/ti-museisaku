---
name: format-conversation-input
description: Normalize ConversationEditor input.txt conversation-node memos into the project's consistent Japanese field order while preserving IDs, text, conditions, and relationships. Use when formatting, normalizing, or adding nodes to ConversationEditor/input.txt; do not use for changing story content or Unity runtime behavior.
---

# Conversation Input Formatter

Format the user-selected conversation memo. When no path is supplied, use `C:\My project\ConversationEditor\input.txt`.

## Required format

Keep node order and use one blank line between nodes. Write every node in this order:

```text
[S01-C001]
想定文: ...
キーワード: ...
返答: ...
キーワード一致条件: ...
前提ノード: ...
インターロッキングノード: ...
返答後に入力待ち: ...
```

Apply these field-name mappings:

- `質問例` or `想定入力` -> `想定文`
- `除外ノード` -> `インターロッキングノード`
- `入力待ち` -> `返答後に入力待ち`

Use `: ` consistently after field names. Represent intentional line breaks inside `返答` as the two characters `\n`, keeping the response on one field line.

## Preserve meaning

- Read the entire memo before editing.
- Preserve every node ID, node order, response, condition, prerequisite, interlocking relationship, waiting flag, and explanatory comment unless the user explicitly changes it.
- Do not reorder serialized or conceptual enum values.
- Do not edit Unity assets, source memos, or runtime code unless explicitly requested.
- Use `なし` for an absent keyword, prerequisite, or interlocking node. Do not infer a new relationship.
- If a legacy exact-match node has only `想定入力: 魔法：NAME`, retain that full value as `想定文` and write the actual candidate name without the scenario prefix as `キーワード: NAME`.
- For multiple exact-match candidates separated by `/` or `／`, preserve their order and write them as a Japanese comma-separated keyword list.
- If a value cannot be normalized without changing meaning, preserve the original value and report the ambiguity.

## Keep the editor fallback synchronized

If `ConversationEditor/input-data.js` exists, update its `window.CONVERSATION_INPUT = String.raw\`...\`;` body to exactly match the formatted `input.txt`. This fallback is required when `index.html` is opened through `file://` and the browser cannot fetch the text file.

If a newly introduced field name is not already recognized by `ConversationEditor/editor.js`, update only the parser alias needed to read it. Preserve the existing node layout and link behavior.

## Validate

After formatting:

1. Confirm the node count and node-ID order are unchanged.
2. Confirm every node contains all seven required fields.
3. Confirm `input.txt` and the `String.raw` body in `input-data.js` match after normalizing line endings and ignoring only the final newline.
4. Run `node --check` for each changed JavaScript file.
5. Run `git diff --check -- ConversationEditor`.
6. Report formatting and static checks separately from browser/runtime verification.
