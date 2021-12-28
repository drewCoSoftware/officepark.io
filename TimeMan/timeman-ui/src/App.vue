<template>
  <SiteHeader />
  <HelloWorld msg="Welcome to SPA hell!" count="0"/>
</template>

<script>
import HelloWorld from './components/HelloWorld.vue'
import SiteHeader from './components/SiteHeader.vue'

export default {
  name: 'App',
  components: {
    HelloWorld,
    SiteHeader
  }
}
</script>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 60px;
}
</style>
