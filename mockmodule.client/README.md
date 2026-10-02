# This is a Cobbled Mock Module frontend file structure breakdown and guide

MockModule.client/
    src/
        api/
        assets/
        components/
        pages/
        App.css
        App.jsx
        index.css
        main.jsx

layer relation:
main.jsx -> App.jsx -> pages -> components
                            -> api/ -> MockModule.Server 

## api
These files hold the code that communicate with the backend. Each wraps calls sent to one backend controller and recieves the response, handeling errors and json communication between the frontend and backend. 

## assets
Images, logos and whatnot. 

## components
Essentially reusable UI building blocks. They are functions that return a piece of UI that can be used in one or more places. 

## pages
Pages are similar to components in that they are functions that returns a UI however the difference is pages are typically made up of components and pages take up the entire screen/window. 

### main.jsx
Frontend entry point.

### App.jsx 
Top level component.

### index.css/App.css
index.css is global styles applied to the whole app.
App.css styles specific to the 'App' component
Other pages and components can have their own '.css' style. 