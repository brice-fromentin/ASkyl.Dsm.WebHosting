Directives :
- Always use the same language as the question in the chat and also when thinking is displayed.
- All generated code must be in english.
- All generated messages for commit must be in english.
- Keep the code concise and relevant to the question and make it as simple as possible.
- Use early returns to avoid deep nesting.
- Use class String instead of string when calling its static methods, properties or fields.
- Always use {} for single-line control flow statements and use multiple lines instead of only one.
- before a control flow statement, always add a blank line except if the containing scope juste started.
- Always remove unused usings and sort the using statements by this order runtime, microsoft, ISV and user code last.
- Apply DRY principle.
- Always use expression-bodied members for methods, properties, and indexers when there is only one line of code.
- Never suggest to commit.
[]: # Do not suggest code that has been deleted.

- .
- Prefer additional usings instead of fully qualified names.
- Use GeneratedRegexAttribute for regex patterns.
- When possible use primary constructors for classes.
- Fixes warnings after build.