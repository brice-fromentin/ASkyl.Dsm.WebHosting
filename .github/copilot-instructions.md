Directives :
- Always use the same language as the question in the chat and also when thinking is displayed.
- All generated code must be in english.
- All generated comments must be in english.
- All generated messages for commit must be in english.
- Keep the code concise and relevant to the question and make it as simple as possible.
- Use early returns to avoid deep nesting.
- Use class String instead of string when calling its static methods, properties or fields otherwise use native type string.
- Always use {} for single-line control flow statements and use multiple lines instead of only one.
- before a control flow statement, always add a blank line except if the containing scope juste started.
- Always sort the using statements by this order runtime, microsoft, ISV and user code last.
- Always remove unused usings.
- Apply DRY principles.
- Apply SOLID principles.
- Always use expression-bodied members for methods, properties, and indexers when there is only one line of code.
- Always use simple collection initializers.
- Always use new() when the type can be inferred.
- When a property has both get and set accessors, always use a multi-line format.
- Never suggest to commit.

[]: # Do not suggest code that has been deleted.

- Prefer additional usings instead of fully qualified names.
- Use GeneratedRegexAttribute for regex patterns.
- When possible use primary constructors for classes.
- Fixes warnings after build.

- FluentUI documentation : https://www.fluentui-blazor.net
- Always prefer FluentUI components over HTML elements.
- Always prefer FluentUI icons over other icon libraries.
- Always prefer FluentUI colors over custom colors.
- Always prefer FluentUI spacing over custom spacing.
- Always prefer FluentUI typography over custom typography.
- Never use inline styles, always use FluentUI theming and styling.

- Synology FileStation API documentation : https://global.download.synology.com/download/Document/DeveloperGuide/Synology_File_Station_API_Guide.pdf