import { createApp } from "vue";
import { createPinia } from "pinia";
import App from "./App.vue";
import router from "./router";

// Esto es como tu <BrowserRouter> + <Provider store={...}> envolviendo el App en React
const app = createApp(App);

app.use(createPinia()); // manejo de estado global
app.use(router); // ruteo

app.mount("#app");
