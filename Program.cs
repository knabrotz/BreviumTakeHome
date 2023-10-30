using System.Text;
using Newtonsoft.Json;

using HttpClient client = new();
client.DefaultRequestHeaders.Accept.Clear();

//Reset the system
await startScheduling(client);

//Get existing schedule
List<Appointment> appointments = await getSchedule(client);

//Schedule all the appointments
// While are more, get new appointment from server
bool isAnotherAppointment = true;
while (isAnotherAppointment) {
    AppointmentRequest appointmentRequest = await getAppointmentRequest(client);
    Console.Write("Retrieved request #"+ appointmentRequest.requestId +"\n");

    if(appointmentRequest is null) {
        isAnotherAppointment = false;
    } else {
        // Scheduling Logic to set schedule
        Tuple<DateTime, int> newAppointmentTime = getNewAppointmentInfo(appointmentRequest);

        if (newAppointmentTime is not null) {
            DateTime appointmentTime = newAppointmentTime.Item1;
            int doctorId = newAppointmentTime.Item2;

            //Update the schedule
            //Update on server
            NewAppointment newAppointment = new NewAppointment {
                doctorId = doctorId,
                PersonId =  appointmentRequest.PersonId,
                appointmentTime = appointmentTime,
                isNewPatientAppointment = appointmentRequest.IsNew,
                requestId = appointmentRequest.requestId
            };

            await scheduleAppointment(client, newAppointment);

            //Update on internal representation    
            appointments.Add(new Appointment {
                doctorId = doctorId,
                PersonId =  appointmentRequest.PersonId,
                appointmentTime = appointmentTime,
                isNewPatientAppointment = appointmentRequest.IsNew
            });    

            Console.Write("Scheduled appointment #" + appointmentRequest.requestId + "\n");

        } else {
            Console.Write("Appointment request #" + appointmentRequest.requestId + " was not able to be scheduled\n");
        }

    }
}


    

Tuple<DateTime, int> getNewAppointmentInfo(AppointmentRequest appointmentRequest) {
    
    foreach (DateTime preferredDay in appointmentRequest.PreferredDays) {

        foreach (int doctorId in appointmentRequest.PreferredDocs) {
            int hour;
            int endHour;

            if (appointmentRequest.IsNew) {
                hour = 15;
                endHour = 16;
            } else {
                hour = 8;
                endHour = 16;
            }

            while (hour < endHour + 1) {
                //check if that time and date is taken by a patient with that doctor
                DateTime timeToCheck = preferredDay.AddHours(hour);

                bool hasExistingAppointment = appointments.Any(appointment => appointment.doctorId == doctorId && appointment.appointmentTime == timeToCheck);

                if (!hasExistingAppointment) {
                    return new Tuple<DateTime, int>(timeToCheck, doctorId);
                } 
                hour++;
            }
        }
    }
    return null;
}

// POST to reset system
static async Task startScheduling(HttpClient client) {
    string requestUri = "http://scheduling-interview-2021-265534043.us-west-2.elb.amazonaws.com/api/Scheduling/Start?token=69e6a5bb-7fcf-4112-87c5-9d74887d38a1";
    HttpContent content = new StringContent(string.Empty);

    var response = await client.PostAsync(requestUri, content);
}

// GET initial monthly schedule
static async Task<List<Appointment>> getSchedule(HttpClient client) {
    HttpResponseMessage response = await client.GetAsync(
        "http://scheduling-interview-2021-265534043.us-west-2.elb.amazonaws.com/api/Scheduling/Schedule?token=69e6a5bb-7fcf-4112-87c5-9d74887d38a1"
    );

    if (response.IsSuccessStatusCode)
    {
        string json = await response.Content.ReadAsStringAsync();
        List<Appointment> appointments = JsonConvert.DeserializeObject<List<Appointment>>(json);
        return appointments;
    } else {
        return null;
    }
}

// GET next appointment request
static async Task<AppointmentRequest> getAppointmentRequest(HttpClient client) {
    HttpResponseMessage response = await client.GetAsync(
        "http://scheduling-interview-2021-265534043.us-west-2.elb.amazonaws.com/api/Scheduling/AppointmentRequest?token=69e6a5bb-7fcf-4112-87c5-9d74887d38a1"
    );

    if (response.IsSuccessStatusCode)
    {
        string json = await response.Content.ReadAsStringAsync();

        if (json == "") {
            return null;
        } else {
            AppointmentRequest appointmentRequest = JsonConvert.DeserializeObject<AppointmentRequest>(json);
            return appointmentRequest;
        }
    } else {
        return null;
    }
}

//POST new scheduled appointment
static async Task scheduleAppointment(HttpClient client, NewAppointment appointment) {
    string requestUri = "http://scheduling-interview-2021-265534043.us-west-2.elb.amazonaws.com/api/Scheduling/Start?token=69e6a5bb-7fcf-4112-87c5-9d74887d38a1";

    var json = JsonConvert.SerializeObject(appointment);
    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await client.PostAsync(requestUri, content);
}

public class AppointmentRequest {
    [JsonProperty("requestId")]
    public int requestId {get; set;}

    [JsonProperty("personId")]
    public int PersonId { get; set; }

    [JsonProperty("preferredDays")]
    public List<DateTime?>? PreferredDays { get; set; }

    [JsonProperty("preferredDocs")]
    public List<int?>? PreferredDocs { get; set; }

    [JsonProperty("isNew")]
    public bool IsNew { get; set; }
}

public class Appointment {
    [JsonProperty("doctorId")]
    public int doctorId {get; set;}

    [JsonProperty("personId")]
    public int PersonId { get; set; }

    [JsonProperty("appointmentTime")]
    public DateTime appointmentTime { get; set; }

    [JsonProperty("isNewPatientAppointment")]
    public bool isNewPatientAppointment { get; set; }
}

public class NewAppointment {
    [JsonProperty("doctorId")]
    public int doctorId {get; set;}

    [JsonProperty("personId")]
    public int PersonId { get; set; }

    [JsonProperty("appointmentTime")]
    public DateTime appointmentTime { get; set; }

    [JsonProperty("isNewPatientAppointment")]
    public bool isNewPatientAppointment { get; set; }

    [JsonProperty("requestId")]
    public int requestId {get; set;}
}