# Identity Key Frontend

A React app for the Identity Key API. Upload a file, see your files, download
them and delete them.

The browser never touches a key - it sends the file to the API and the API does
the encrypting and decrypting.

## Built with

React 19 and Vite 8.

## Running it

The API has to be running first, on `http://localhost:5001`.

```powershell
npm install
npm run dev
```

Then open http://localhost:5173.

### Scripts

| Script | Does |
| --- | --- |
| `npm run dev` | start the dev server on port 5173 |
| `npm run build` | production build into `dist/` |
| `npm run lint` | run ESLint |
| `npm run preview` | serve the production build |

## Project structure

```
identity-key-frontend/
├─ vite.config.js          dev server and the /api proxy
├─ index.html              page shell
└─ src/
   ├─ main.jsx             entry point
   ├─ App.jsx              renders the page
   ├─ api/fileApi.js       all the calls to the API
   ├─ utils/fileFormat.js  file size and extension helpers
   ├─ pages/FilePage.jsx   page state and the four actions
   └─ components/
      ├─ FileUploadForm.jsx  drop zone and upload button
      ├─ FileList.jsx        search box and the list itself
      └─ FileItem.jsx        one file row
```

## How it fits together

`FilePage` holds all the state (the file list, the search text, the busy flags
and the messages) and passes it down. The components underneath just display
things and call back up.

There are four actions, one per API call: load, upload, download and delete.

## Notes

- Requests use a relative path (`/api/...`) and Vite proxies them to the backend.
  See `vite.config.js`. This keeps the browser on a single origin, so there is
  no CORS setup needed. If the API is down, the error shows up in the Vite
  terminal rather than as a vague "Failed to fetch" in the browser.
- Changing `vite.config.js` needs a dev server restart - hot reload does not
  pick it up.
- The tenant headers are hard-coded in `src/api/fileApi.js` because there is no
  login screen yet.
- Uploads are limited to 25 MB, checked in the form and again by the API.
- Search is done in the browser - it matches the name, type or status.
