<template>
  <SiteHeader v-bind:loginState="loginState" />
  <HelloWorld msg="Welcome to SPA hell!" count="0"/>
  <button v-on:click="toggleLogin()">Toggle Login</button>
</template>

<script>
import HelloWorld from './components/HelloWorld.vue'
import SiteHeader from './components/SiteHeader.vue'

export default {
  name: 'App',
  components: {
    HelloWorld,
    SiteHeader
  },
  data() {
      return {
        loginState: {
          isLoggedIn: true
        }
      }
  },
  methods: {
    'toggleLogin': function(){
      this.loginState.isLoggedIn = !this.loginState.isLoggedIn;
//      alert('login changed! (' + this.isLoggedIn + ')');
    }
  }
  // setup (){
  //   this.isLoggedIn = false;
  // }
  // data() {
  //   return new {
  //     isLoggedIn: false
  //   }
  // }
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
