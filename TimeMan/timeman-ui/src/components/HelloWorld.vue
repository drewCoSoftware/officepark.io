<template>
  <div class="hello">
    <h1>{{ msg }}</h1>
    <p>Count is: {{ count }}</p>
    <button v-on:click="getSessions">Push me!</button>

    <time-man-session
      v-for="item in sessions"
      v-bind:sess="item"
      v-bind:key="item.id"
    ></time-man-session>
  </div>
</template>

<script>
import TimeManSession from "./TimeManSession.vue";

export default {
  components: { TimeManSession },
  name: "HelloWorld",
  props: {
    msg: String,
  },
  data() {
    return {
      count: 0,
      sessions: [
        { id: 0, sessionName: "Session 1" },
        { id: 1, sessionName: "Session 2" },
        { id: 2, sessionName: "Session 3" },
      ],
    };
  },
  methods: {
    increment: function () {
      this.count++;
    },
    getSessions: function () {
     // let t = this;
      let p = fetch("https://localhost:7001/api/sessions/active");
      p.then(response => response.json()).then(data =>{
        this.sessions = data;
      });
        
        // (t.sessions = [{ id: 10, sessionName: "Fetched Session!" }])
        //);
    },
  },
};
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
h3 {
  margin: 40px 0 0;
}
ul {
  list-style-type: none;
  padding: 0;
}
li {
  display: inline-block;
  margin: 0 10px;
}
a {
  color: #42b983;
}
</style>
