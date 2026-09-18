# Third-party notices

Everything in this repository is licensed under the MIT license in [LICENSE](LICENSE),
**except** for the application icon:

- `assets/icon/hookchime.ico`
- `assets/icon/hookchime.png`

These are derived from the "NotificationAlert" image in the **Microsoft Visual Studio
Image Library** and are used under the terms of the Microsoft Visual Studio 2022 Image
Library license agreement, a copy of which is included at
[`licenses/Visual Studio 2022 Image Library EULA.rtf`](licenses/Visual%20Studio%202022%20Image%20Library%20EULA.rtf).

That license is **not** MIT, and its terms take precedence over the repository's MIT
license for these two files specifically. In short: it permits use and distribution of
the image as part of an application with real functionality of its own (which HookChime
has), but it does not permit the image to be extracted and redistributed on its own,
used to imply Microsoft endorsement of this project, or placed under a license that
grants recipients unrestricted modification/redistribution rights the way MIT normally
would for the rest of this repo.

If you fork or redistribute HookChime and don't want to deal with those terms, replace
`assets/icon/hookchime.ico` and `assets/icon/hookchime.png` with your own icon — nothing
else in the codebase depends on their specific content, only their file paths.
