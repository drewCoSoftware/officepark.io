import { createApp } from 'vue'
import App from './App.vue'

import Home from "./components/Home.vue"
import About from "./components/About.vue"
import Sessions from "./components/Sessions.vue"
import NotFound from "./components/NotFound.vue"

import { createRouter, createWebHistory } from "vue-router";

// https://www.npmjs.com/package/vue3-cookies
import VueCookies from 'vue3-cookies'

const routes = [
  {
    path: "/",
    name: "Home",
    component: Home,
  },
  {
    path: "/about",
    name: "About",
    component: About,
  },
  {
    path: "/sessions",
    name: "Sessions",
    component: Sessions,
  },
  {
      path: "/:notfound(.*)",
      name: "NotFound",
      component: NotFound,
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;

const app = createApp(App);
app.use(router);
app.use(VueCookies, {
  expireTimes: "30d",
  path: "/",
  domain: "",
  secure: true,
  sameSite: "None"
});
app.mount('#app')


