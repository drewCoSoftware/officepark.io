import { createApp } from 'vue'
import App from './App.vue'

import Home from "./components/Home.vue"
import About from "./components/About.vue"

import { createRouter,createWebHistory } from "vue-router";

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
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router

const app = createApp(App);
app.use(router);
app.mount('#app')


